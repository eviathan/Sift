using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sift.Core.Commands;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;
using Sift.Core.Nodes;
using Sift.Core.Serialisation;
using Sift.Core.Snapshots;

namespace Sift.Core
{
    // NOTE: Owns virtual time. Nothing in here knows about the wall clock, the Sequencer drives it via AdvanceTo.
    // Runtime state is only touched under _sync (the clock thread while playing), everything else submits to _pending
    public class SequencerRuntime : ISequencerContext
    {
        private const int STOP_TIMEOUT_IN_MS = 500;

        private readonly ILogger<SequencerRuntime> _logger;
        private readonly Sequence _sequence;
        private readonly EventScheduler _scheduler;
        private readonly SpatialPropagator _spatialPropagator;
        private readonly RuntimeEventHub _events;
        private readonly ComponentRegistry _registry;

        private readonly object _sync = new();
        private readonly ConcurrentQueue<PendingItem> _pending = new();
        private readonly Dictionary<Guid, InFlightMessage> _inFlight = [];
        private readonly Stack<IGridCommand> _undo = new();
        private readonly Stack<IGridCommand> _redo = new();

        // NOTE: Components are fire and forget by design, some loop and need to run concurrently
        private readonly ConcurrentDictionary<Task, byte> _activeComponentTasks = new();
        private CancellationTokenSource _playbackCancellationTokenSource = new();

        // NOTE: Values the components on each node share, for this playback
        private readonly ConcurrentDictionary<Guid, NodeState> _nodeStates = new();

        // NOTE: Grid edits made by components while playing, undone on Stop so playing never changes the saved sequence
        private readonly Stack<IGridCommand> _performanceEdits = new();

        // NOTE: Per component so removing one (or its node) cancels anything it still has running
        private readonly Dictionary<Guid, CancellationTokenSource> _componentCancellation = [];

        // NOTE: Guarded separately from _sync so components can register waits without contending with the clock
        private readonly object _waitersSync = new();
        private readonly PriorityQueue<(TaskCompletionSource<Time> Completion, Time DueTime), (long Ticks, long Order)> _waiters = new();
        private long _nextWaiterOrder;

        private long _currentTicks;
        private volatile bool _isRunning;

        public Time Now => new(Volatile.Read(ref _currentTicks));

        public bool IsRunning => _isRunning;

        private Time CurrentTime
        {
            get => new(_currentTicks);
            set => Volatile.Write(ref _currentTicks, value.Ticks);
        }

        public SequencerRuntime(ILogger<SequencerRuntime> logger, Sequence sequence, EventScheduler scheduler, SpatialPropagator spatialPropagator, RuntimeEventHub events, ComponentRegistry registry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _spatialPropagator = spatialPropagator ?? throw new ArgumentNullException(nameof(spatialPropagator));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_isRunning)
                    return;

                Reset();

                // NOTE: Applies edits submitted while stopped and drops any stale emits
                Drain();

                _playbackCancellationTokenSource.Dispose();
                _playbackCancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                var playNodes = _sequence.Grid.Values.Where(x => x.EmitOnPlay && x.IsActive).ToList();

                foreach (var node in playNodes)
                    _scheduler.Schedule(CurrentTime, new Message { Payload = "play" }, node.Id, null);
            }
        }

        public void Stop()
        {
            Task[] runningTasks;

            lock (_sync)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _playbackCancellationTokenSource.Cancel();
                runningTasks = _activeComponentTasks.Keys.ToArray();

                // NOTE: Edits still queued from components die with the playback
                Drain();
                RevertPerformanceEdits();
                Reset();
            }

            // NOTE: Wait outside the lock, components may still be submitting as they wind down
            try
            {
                if (!Task.WhenAll(runningTasks).Wait(STOP_TIMEOUT_IN_MS))
                    _logger.LogWarning($"{runningTasks.Count(x => !x.IsCompleted)} component(s) did not stop within {STOP_TIMEOUT_IN_MS}ms of cancellation.");
            }
            catch (AggregateException)
            {
                // NOTE: Faults are logged as each task completes, cancellations are expected
            }
        }

        public void Emit(Guid sourceNodeId, Direction direction, Message message, Time? at = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            _pending.Enqueue(new PendingEmit(sourceNodeId, direction, message, at ?? Now));
        }

        // NOTE: For the user (UI/CLI). Recorded for undo. Applied immediately when stopped, at the next tick when playing
        public Task Submit(IGridCommand command) =>
            Enqueue(command, CommandSource.User);

        Task ISequencerContext.Submit(IGridCommand command) =>
            Enqueue(command, CommandSource.Component);

        public Task<bool> Undo() =>
            EnqueueHistory(isUndo: true);

        public Task<bool> Redo() =>
            EnqueueHistory(isUndo: false);

        // NOTE: Swaps in a whole sequence (e.g. one just opened). Only while stopped, and the undo history goes with
        // the old sequence
        public void Replace(LoadedSequence loaded)
        {
            ArgumentNullException.ThrowIfNull(loaded);

            lock (_sync)
            {
                if (_isRunning)
                    throw new InvalidOperationException("Stop playback before replacing the sequence.");

                // NOTE: Edits queued for the old sequence no longer apply
                Drain();

                _sequence.Grid.Clear();

                foreach (var node in loaded.Nodes)
                    _sequence.Grid.AddNode(node);

                _sequence.Tempo = (ushort)Math.Clamp(loaded.Tempo, 1, ushort.MaxValue);
                _sequence.Quantisation = loaded.Quantisation;

                _undo.Clear();
                _redo.Clear();

                Raise(new SequenceReplaced());
                RaiseHistoryChanged();
            }
        }

        // NOTE: Applied in order with commands, so the snapshot plus every later event is always consistent
        public Task<SequenceSnapshot> GetSnapshotAsync()
        {
            var completion = new TaskCompletionSource<SequenceSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Enqueue(new PendingSnapshot(completion));

            DrainIfStopped();

            return completion.Task;
        }

        public void Publish(RuntimeEvent runtimeEvent)
        {
            ArgumentNullException.ThrowIfNull(runtimeEvent);
            _events.Publish(runtimeEvent, Now);
        }

        public Task<Time> WaitTicksAsync(long ticks, CancellationToken cancellationToken) =>
            WaitUntilAsync(Now + Math.Max(0, ticks), cancellationToken);

        public Task<Time> WaitUntilAsync(Time time, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Time>(cancellationToken);

            var completion = new TaskCompletionSource<Time>(TaskCreationOptions.RunContinuationsAsynchronously);

            // NOTE: Checked under the waiter lock so a wait can't slip in after Stop has cancelled the rest
            lock (_waitersSync)
            {
                if (!_isRunning)
                    return Task.FromCanceled<Time>(new CancellationToken(canceled: true));

                _waiters.Enqueue((completion, time), (time.Ticks, ++_nextWaiterOrder));
            }

            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
            }

            return completion.Task;
        }

        public void AdvanceTo(Time targetTime)
        {
            lock (_sync)
            {
                if (!_isRunning)
                    return;

                Drain();

                while (true)
                {
                    while (_scheduler.TryDequeueDue(CurrentTime, out var evt))
                        Dispatch(evt!);

                    ReleaseWaiters();

                    // NOTE: Picks up what this tick's components emitted so the jump below can't skip their arrivals
                    Drain();

                    var nextDueTicks = GetNextDueTicks();

                    // NOTE: Late emits and retargets can land on the current tick, handle them before moving on
                    if (nextDueTicks <= CurrentTime.Ticks)
                        continue;

                    if (CurrentTime.Ticks >= targetTime.Ticks)
                        break;

                    // NOTE: Jump straight to the next event/wait rather than stepping through empty ticks
                    CurrentTime = new Time(Math.Min(targetTime.Ticks, nextDueTicks));
                }
            }
        }

        // NOTE: Lets the clock spin rather than sleep when something is about to happen. Null when nothing is scheduled
        public Time? NextDueTime
        {
            get
            {
                lock (_sync)
                {
                    var nextDueTicks = GetNextDueTicks();
                    return nextDueTicks == long.MaxValue ? null : new Time(nextDueTicks);
                }
            }
        }

        private long GetNextDueTicks()
        {
            var nextDueTicks = _scheduler.TryPeekDueTime(out var dueTime) ? dueTime.Ticks : long.MaxValue;

            lock (_waitersSync)
            {
                if (_waiters.TryPeek(out _, out var priority))
                    nextDueTicks = Math.Min(nextDueTicks, priority.Ticks);
            }

            return nextDueTicks;
        }

        private Task Enqueue(IGridCommand command, CommandSource source)
        {
            ArgumentNullException.ThrowIfNull(command);

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Enqueue(new PendingCommand(command, source, completion));

            DrainIfStopped();

            return completion.Task;
        }

        private Task<bool> EnqueueHistory(bool isUndo)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Enqueue(new PendingHistory(isUndo, completion));

            DrainIfStopped();

            return completion.Task;
        }

        private void DrainIfStopped()
        {
            if (_isRunning)
                return;

            lock (_sync)
            {
                if (!_isRunning)
                    Drain();
            }
        }

        private void Drain()
        {
            var spaceChanged = false;

            while (_pending.TryDequeue(out var item))
            {
                switch (item)
                {
                    case PendingEmit emit:
                        if (_isRunning)
                            Launch(emit);
                        break;

                    case PendingCommand command:
                        spaceChanged |= ApplyCommand(command);
                        break;

                    case PendingHistory history:
                        spaceChanged |= ApplyHistory(history);
                        break;

                    case PendingSnapshot snapshot:
                        // NOTE: Retarget first so the snapshot's in-flight messages match the grid
                        RetargetIfChanged(ref spaceChanged);
                        snapshot.Completion.TrySetResult(CreateSnapshot());
                        break;
                }
            }

            RetargetIfChanged(ref spaceChanged);
        }

        private void RetargetIfChanged(ref bool spaceChanged)
        {
            if (!spaceChanged)
                return;

            spaceChanged = false;

            foreach (var message in _inFlight.Values)
            {
                if (Resolve(message))
                    Raise(new MessageRetargeted(message.Id, message.TargetNodeId, message.EndPosition, message.ArrivalTime));
            }
        }

        private SequenceSnapshot CreateSnapshot() =>
            new(_events.LastSequence,
                CurrentTime,
                _isRunning,
                _sequence.Tempo,
                _sequence.Quantisation,
                _sequence.TicksPerCell,
                _sequence.Grid.Values.Select(x => SnapshotFactory.Create(x, _registry)).ToList(),
                _inFlight.Values.Select(SnapshotFactory.Create).ToList(),
                _undo.Count > 0,
                _redo.Count > 0);

        private void Raise(RuntimeEvent runtimeEvent)
        {
            if (_events.HasSubscribers)
                _events.Publish(runtimeEvent, CurrentTime);
        }

        private bool ApplyCommand(PendingCommand pending)
        {
            if (pending.Source == CommandSource.Component && !_isRunning)
            {
                pending.Completion.TrySetCanceled();
                return false;
            }

            var before = CaptureNode(pending.Command.NodeId);

            try
            {
                var inverse = pending.Command.Apply(_sequence.Grid);

                if (pending.Source == CommandSource.User)
                {
                    _undo.Push(inverse);
                    _redo.Clear();
                }
                else
                {
                    _performanceEdits.Push(inverse);
                }

                CancelComponents(GetDetachedComponents(inverse));
                RaiseNodeChange(pending.Command.NodeId, before, pending.Source);

                if (pending.Source == CommandSource.User)
                    RaiseHistoryChanged();

                pending.Completion.TrySetResult();
                return pending.Command.AffectsSpace;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Tick {CurrentTime.Ticks}] {pending.Source} command {pending.Command.GetType().Name} rejected: {ex.Message}");
                Raise(new CommandRejected(pending.Command, pending.Source, ex.Message));
                pending.Completion.TrySetException(ex);
                return false;
            }
        }

        private bool ApplyHistory(PendingHistory pending)
        {
            var (from, to) = pending.IsUndo ? (_undo, _redo) : (_redo, _undo);

            if (!from.TryPop(out var command))
            {
                pending.Completion.TrySetResult(false);
                return false;
            }

            var before = CaptureNode(command.NodeId);

            try
            {
                var inverse = command.Apply(_sequence.Grid);
                to.Push(inverse);

                CancelComponents(GetDetachedComponents(inverse));
                RaiseNodeChange(command.NodeId, before, CommandSource.User);
                RaiseHistoryChanged();

                pending.Completion.TrySetResult(true);
                return command.AffectsSpace;
            }
            catch (Exception ex)
            {
                // NOTE: The grid has moved on (e.g. a component changed it) so this entry can no longer be applied, drop it
                _logger.LogWarning($"[Tick {CurrentTime.Ticks}] {(pending.IsUndo ? "Undo" : "Redo")} of {command.GetType().Name} failed: {ex.Message}");
                Raise(new CommandRejected(command, CommandSource.User, ex.Message));
                RaiseHistoryChanged();

                pending.Completion.TrySetException(ex);
                return false;
            }
        }

        // NOTE: Last first, skipping any the user's own edits have made impossible (e.g. they deleted the node)
        private void RevertPerformanceEdits()
        {
            while (_performanceEdits.TryPop(out var inverse))
            {
                var before = CaptureNode(inverse.NodeId);

                try
                {
                    inverse.Apply(_sequence.Grid);
                    RaiseNodeChange(inverse.NodeId, before, CommandSource.Component);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Couldn't revert a {inverse.GetType().Name} made while playing: {ex.Message}");
                }
            }
        }

        private (bool Existed, Vector3D Position) CaptureNode(Guid nodeId) =>
            _sequence.Grid.TryGetNode(nodeId, out var node) ? (true, node.Position) : (false, default);

        // NOTE: Works out what a command did by comparing the node before and after, so new commands need no extra wiring
        private void RaiseNodeChange(Guid nodeId, (bool Existed, Vector3D Position) before, CommandSource source)
        {
            if (!_events.HasSubscribers)
                return;

            var exists = _sequence.Grid.TryGetNode(nodeId, out var node);

            if (!before.Existed && exists)
                Raise(new NodeAdded(SnapshotFactory.Create(node, _registry), source));
            else if (before.Existed && !exists)
                Raise(new NodeRemoved(nodeId, source));
            else if (exists && node.Position != before.Position)
                Raise(new NodeMoved(nodeId, before.Position, node.Position, source));
            else if (exists)
                Raise(new NodeChanged(SnapshotFactory.Create(node, _registry), source));
        }

        private void RaiseHistoryChanged() =>
            Raise(new HistoryChanged(_undo.Count > 0, _redo.Count > 0));

        private void Launch(PendingEmit emit)
        {
            if (!_sequence.Grid.TryGetNode(emit.SourceNodeId, out var node))
                return;

            var message = new InFlightMessage(emit.SourceNodeId, node.Position, emit.Direction, emit.EmitTime, _sequence.TicksPerCell, emit.Message);
            _inFlight[message.Id] = message;

            _logger.LogInformation($"[Tick {emit.EmitTime.Ticks}] Node {emit.SourceNodeId} emitting '{emit.Message.Payload}' via {emit.Direction}");

            Resolve(message, isLaunch: true);

            Raise(new MessageEmitted(
                message.Id,
                message.SourceNodeId,
                message.Origin,
                message.Direction,
                message.EmitTime,
                message.TicksPerCell,
                message.Message.Payload,
                message.TargetNodeId,
                message.EndPosition,
                message.ArrivalTime));
        }

        // NOTE: Finds the first node ahead of where the message currently is and (re)schedules its arrival.
        // Returns false when nothing changed
        private bool Resolve(InFlightMessage message, bool isLaunch = false)
        {
            // NOTE: A launch never skips cells. Concurrent components can emit for a tick the clock has already passed,
            // that message should arrive late rather than fly straight through its target
            var travelledTicks = isLaunch ? 0 : CurrentTime.Ticks - message.EmitTime.Ticks;
            var minDistance = (int)Math.Clamp((travelledTicks + message.TicksPerCell - 1) / message.TicksPerCell, 1, SpatialPropagator.MAX_RAYCAST_DISTANCE + 1);

            Guid? targetNodeId = null;
            var distance = SpatialPropagator.MAX_RAYCAST_DISTANCE;

            if (_spatialPropagator.TryRaycast(_sequence.Grid, message.Origin, message.Direction, message.TicksPerCell, message.EmitTime, out var result, minDistance))
            {
                targetNodeId = result.NodeId;
                distance = result.Distance;
            }

            var isRetarget = message.Version > 0;

            if (isRetarget && targetNodeId == message.TargetNodeId && distance == message.Distance)
                return false;

            message.TargetNodeId = targetNodeId;
            message.Distance = distance;
            message.ArrivalTime = message.EmitTime + checked(distance * message.TicksPerCell);
            message.Version++;

            _scheduler.Schedule(message.ArrivalTime, message.Message, targetNodeId, message.TargetPortDirection, message.Id, message.Version);

            var prefix = isRetarget ? $"[Tick {CurrentTime.Ticks}] Message {message.Id} retargeted" : " ->";

            if (targetNodeId != null)
                _logger.LogInformation($"{prefix} Raycast hit Node {targetNodeId} at distance {distance} cells. Scheduled arrival for Tick {message.ArrivalTime.Ticks}.");
            else
                _logger.LogInformation($"{prefix} Raycast hit nothing in space. Message will dissipate at Tick {message.ArrivalTime.Ticks}.");

            return true;
        }

        private void Dispatch(NodeEvent nodeEvent)
        {
            InFlightMessage? message = null;

            if (nodeEvent.InFlightId is { } inFlightId)
            {
                // NOTE: Stale arrival, the message was retargeted after this was scheduled
                if (!_inFlight.TryGetValue(inFlightId, out message) || message.Version != nodeEvent.InFlightVersion)
                    return;

                _inFlight.Remove(inFlightId);
            }

            if (nodeEvent.TargetNodeId is not { } targetNodeId)
            {
                _logger.LogInformation($"[Tick {CurrentTime.Ticks}] Message '{nodeEvent.Message.Payload}' dissipated.");

                if (message != null)
                    Raise(new MessageDissipated(message.Id, message.EndPosition));

                return;
            }

            if (!_sequence.Grid.TryGetNode(targetNodeId, out var node))
                return;

            var isPlay = nodeEvent.TargetPortDirection == null;
            var nodeInfo = SnapshotFactory.CreateInfo(node);

            _logger.LogInformation($"[Tick {CurrentTime.Ticks}] Dispatching '{nodeEvent.Message.Payload}' to Node {targetNodeId} {(isPlay ? "all ports (play)" : $"Port {nodeEvent.TargetPortDirection}")}");

            if (nodeEvent.TargetPortDirection is { } direction)
            {
                if (message != null)
                    Raise(new MessageDelivered(message.Id, node.Id, direction));

                InvokePort(node, nodeInfo, direction, isPlay, nodeEvent.Message);
            }
            else
            {
                Raise(new NodeTriggeredByPlay(node.Id));

                foreach (var portDirection in node.Bus.Keys)
                    InvokePort(node, nodeInfo, portDirection, isPlay, nodeEvent.Message);
            }
        }

        private void InvokePort(Node node, NodeInfo nodeInfo, Direction direction, bool isPlay, Message message)
        {
            // NOTE: Dispatch on the current tick rather than DueTime so late events never move time backwards
            var context = new ComponentContext(nodeInfo, direction, isPlay, message, CurrentTime, this, _nodeStates.GetOrAdd(node.Id, _ => new NodeState()));

            foreach (var component in node.GetPort(direction).Components)
            {
                var invocation = new Invocation(Guid.NewGuid(), node.Id, direction, component);
                Raise(new ComponentStarted(invocation.Id, node.Id, direction, component.Id));

                try
                {
                    Track(component.InvokeAsync(context, GetComponentToken(component)), invocation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Component {component.GetType().Name} on Node {node.Id} Port {direction} threw during dispatch.");
                    Raise(new ComponentFinished(invocation.Id, node.Id, direction, component.Id, ComponentOutcome.Faulted, ex.Message));
                }
            }
        }

        private void Track(Task task, Invocation invocation)
        {
            if (task.IsCompleted)
            {
                OnComponentFinished(task, invocation);
                return;
            }

            _activeComponentTasks.TryAdd(task, 0);

            task.ContinueWith(completed =>
            {
                _activeComponentTasks.TryRemove(completed, out _);
                OnComponentFinished(completed, invocation);
            }, TaskScheduler.Default);
        }

        // NOTE: Can run on the thread pool, so publishes with Now rather than touching CurrentTime
        private void OnComponentFinished(Task task, Invocation invocation)
        {
            var outcome = task.Status switch
            {
                TaskStatus.Faulted => ComponentOutcome.Faulted,
                TaskStatus.Canceled => ComponentOutcome.Cancelled,
                _ => ComponentOutcome.Completed
            };

            var error = task.Exception?.GetBaseException();

            if (error != null)
                _logger.LogError(error, $"Component {invocation.Component.GetType().Name} on Node {invocation.NodeId} Port {invocation.Port} faulted.");

            if (_events.HasSubscribers)
                Publish(new ComponentFinished(invocation.Id, invocation.NodeId, invocation.Port, invocation.Component.Id, outcome, error?.Message));
        }

        private void ReleaseWaiters()
        {
            List<(TaskCompletionSource<Time> Completion, Time DueTime)>? due = null;

            lock (_waitersSync)
            {
                while (_waiters.TryPeek(out _, out var priority) && priority.Ticks <= CurrentTime.Ticks)
                    (due ??= []).Add(_waiters.Dequeue());
            }

            // NOTE: Completed outside the lock, continuations run on the thread pool (RunContinuationsAsynchronously)
            if (due != null)
            {
                foreach (var waiter in due)
                    waiter.Completion.TrySetResult(waiter.DueTime);
            }
        }

        private CancellationToken GetComponentToken(IComponent component)
        {
            if (!_componentCancellation.TryGetValue(component.Id, out var cancellation))
            {
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(_playbackCancellationTokenSource.Token);
                _componentCancellation[component.Id] = cancellation;
            }

            return cancellation.Token;
        }

        private void CancelComponents(IEnumerable<IComponent> components)
        {
            foreach (var component in components)
            {
                // NOTE: Removed so a component that is re-attached (e.g. undo) gets a fresh token on its next invocation
                if (_componentCancellation.Remove(component.Id, out var cancellation))
                    cancellation.Cancel();
            }
        }

        // NOTE: A command that detaches components returns an inverse that re-attaches them
        private static IEnumerable<IComponent> GetDetachedComponents(IGridCommand inverse) => inverse switch
        {
            AddNode addNode => addNode.Node.Bus.Values.SelectMany(x => x.Components).ToList(),
            AddComponent addComponent => [addComponent.Component],
            _ => []
        };

        // NOTE: Leaves _pending and undo history alone, they belong to the user's edits not the playback
        private void Reset()
        {
            _scheduler.Clear();
            _inFlight.Clear();
            _activeComponentTasks.Clear();
            _componentCancellation.Clear();
            _nodeStates.Clear();

            lock (_waitersSync)
            {
                while (_waiters.TryDequeue(out var waiter, out _))
                    waiter.Completion.TrySetCanceled();
            }

            CurrentTime = Time.Zero;
        }

        private abstract record PendingItem;

        private sealed record PendingEmit(Guid SourceNodeId, Direction Direction, Message Message, Time EmitTime) : PendingItem;

        private sealed record PendingCommand(IGridCommand Command, CommandSource Source, TaskCompletionSource Completion) : PendingItem;

        private sealed record PendingHistory(bool IsUndo, TaskCompletionSource<bool> Completion) : PendingItem;

        private sealed record PendingSnapshot(TaskCompletionSource<SequenceSnapshot> Completion) : PendingItem;

        private sealed record Invocation(Guid Id, Guid NodeId, Direction Port, IComponent Component);
    }
}
