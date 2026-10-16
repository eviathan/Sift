using System.Diagnostics;
using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Extensions;
using Sift.Core.Grid;
using Sift.Core.Snapshots;

namespace Sift.App.Scene
{
    // NOTE: The UI's copy of the runtime, built from a snapshot then kept current by applying events. Only touched on
    // the UI thread, so the renderer can read it every frame without locking
    public sealed class SceneModel
    {
        // NOTE: How long a node stays lit after something reaches it, in seconds
        public const double FLASH_SECONDS = 0.35;

        public sealed record MessageView(
            Guid Id,
            Vector3D Origin,
            Direction Direction,
            Time EmitTime,
            long TicksPerCell,
            string Payload,
            Guid? TargetNodeId,
            Vector3D EndPosition,
            Time ArrivalTime);

        public sealed record Connection(Guid NodeId, Vector3D From, Direction Direction, Vector3D To, bool HasTarget);

        public readonly record struct Flash(Vector3D Position, double StartedAt, bool IsPlay, bool IsDissipation);

        private readonly Dictionary<Guid, NodeSnapshot> _nodes = [];
        private readonly Dictionary<Guid, MessageView> _messages = [];
        private readonly Dictionary<Guid, int> _soundingNotes = [];
        private readonly Dictionary<Guid, int> _runningComponents = [];
        private readonly List<Flash> _flashes = [];
        private List<Connection>? _connections;

        public IReadOnlyCollection<NodeSnapshot> Nodes => _nodes.Values;
        public IReadOnlyCollection<MessageView> Messages => _messages.Values;
        public IReadOnlyList<Flash> Flashes => _flashes;

        public long LastSequence { get; private set; }

        public bool IsPlaying { get; private set; }
        public int Tempo { get; private set; }
        public long TicksPerCell { get; private set; } = Time.TicksPerQuarterNote;
        public bool CanUndo { get; private set; }
        public bool CanRedo { get; private set; }

        // NOTE: The subscriber fell behind, the host takes a new snapshot
        public bool NeedsResync { get; private set; }

        private double _transportPosition;
        private double _transportTicksPerSecond;
        private long _transportTimestamp;

        // NOTE: Bumped whenever nodes change so dependants (bounds, the sidebar) know to refresh
        public int NodesVersion { get; private set; }

        public void Load(SequenceSnapshot snapshot)
        {
            _nodes.Clear();
            _messages.Clear();
            _soundingNotes.Clear();
            _runningComponents.Clear();
            _flashes.Clear();

            foreach (var node in snapshot.Nodes)
                _nodes[node.Id] = node;

            foreach (var message in snapshot.InFlight)
                _messages[message.MessageId] = new MessageView(message.MessageId, message.Origin, message.Direction, message.EmitTime,
                    message.TicksPerCell, message.Payload, message.TargetNodeId, message.EndPosition, message.ArrivalTime);

            LastSequence = snapshot.LastEventSequence;
            IsPlaying = snapshot.IsPlaying;
            Tempo = snapshot.Tempo;
            TicksPerCell = snapshot.TicksPerCell;
            CanUndo = snapshot.CanUndo;
            CanRedo = snapshot.CanRedo;
            NeedsResync = false;

            _transportPosition = snapshot.Tick.Ticks;
            _transportTicksPerSecond = snapshot.IsPlaying ? snapshot.Tempo / 60.0 * Time.TicksPerQuarterNote : 0;
            _transportTimestamp = Stopwatch.GetTimestamp();

            NodesChanged();
        }

        // NOTE: Returns true when something the Paper UI shows changed (as opposed to just the scene)
        public bool Apply(RuntimeEvent runtimeEvent)
        {
            if (runtimeEvent.Sequence <= LastSequence)
                return false;

            LastSequence = runtimeEvent.Sequence;
            var now = Seconds;

            switch (runtimeEvent)
            {
                case TransportChanged transport:
                    IsPlaying = transport.IsPlaying;
                    Tempo = transport.Tempo;
                    _transportPosition = transport.Position;
                    _transportTicksPerSecond = transport.IsPlaying ? transport.TicksPerSecond : 0;
                    _transportTimestamp = transport.Timestamp;

                    // NOTE: Stopping clears everything in flight
                    if (!transport.IsPlaying)
                    {
                        _messages.Clear();
                        _soundingNotes.Clear();
                        _runningComponents.Clear();
                    }

                    return true;

                case MessageEmitted emitted:
                    _messages[emitted.MessageId] = new MessageView(emitted.MessageId, emitted.Origin, emitted.Direction, emitted.EmitTime,
                        emitted.TicksPerCell, emitted.Payload, emitted.TargetNodeId, emitted.EndPosition, emitted.ArrivalTime);
                    return false;

                case MessageRetargeted retargeted:
                    if (_messages.TryGetValue(retargeted.MessageId, out var message))
                        _messages[message.Id] = message with { TargetNodeId = retargeted.TargetNodeId, EndPosition = retargeted.EndPosition, ArrivalTime = retargeted.ArrivalTime };
                    return false;

                case MessageDelivered delivered:
                    _messages.Remove(delivered.MessageId);

                    if (_nodes.TryGetValue(delivered.NodeId, out var target))
                        _flashes.Add(new Flash(target.Position, now, IsPlay: false, IsDissipation: false));
                    return false;

                case MessageDissipated dissipated:
                    _messages.Remove(dissipated.MessageId);
                    _flashes.Add(new Flash(dissipated.Position, now, IsPlay: false, IsDissipation: true));
                    return false;

                case NodeTriggeredByPlay played:
                    if (_nodes.TryGetValue(played.NodeId, out var playedNode))
                        _flashes.Add(new Flash(playedNode.Position, now, IsPlay: true, IsDissipation: false));
                    return false;

                case ComponentStarted started:
                    _runningComponents[started.NodeId] = _runningComponents.GetValueOrDefault(started.NodeId) + 1;
                    return false;

                case ComponentFinished finished:
                    Decrement(_runningComponents, finished.NodeId);
                    return false;

                case NoteOn noteOn:
                    _soundingNotes[noteOn.NodeId] = _soundingNotes.GetValueOrDefault(noteOn.NodeId) + 1;
                    return false;

                case NoteOff noteOff:
                    Decrement(_soundingNotes, noteOff.NodeId);
                    return false;

                case NodeAdded added:
                    _nodes[added.Node.Id] = added.Node;
                    NodesChanged();
                    return true;

                case NodeChanged changed:
                    _nodes[changed.Node.Id] = changed.Node;
                    NodesChanged();
                    return true;

                case NodeMoved moved:
                    if (_nodes.TryGetValue(moved.NodeId, out var movedNode))
                        _nodes[moved.NodeId] = movedNode with { Position = moved.To };
                    NodesChanged();
                    return true;

                case NodeRemoved removed:
                    _nodes.Remove(removed.NodeId);
                    NodesChanged();
                    return true;

                case HistoryChanged history:
                    CanUndo = history.CanUndo;
                    CanRedo = history.CanRedo;
                    return true;

                case EventsDropped:
                    NeedsResync = true;
                    return false;
            }

            return false;
        }

        public static double Seconds => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        // NOTE: Extrapolated from the last transport event so the playhead and messages move smoothly between events
        public double CurrentTick =>
            _transportTicksPerSecond <= 0
                ? _transportPosition
                : _transportPosition + Stopwatch.GetElapsedTime(_transportTimestamp).TotalSeconds * _transportTicksPerSecond;

        public bool IsSounding(Guid nodeId) =>
            _soundingNotes.GetValueOrDefault(nodeId) > 0;

        // NOTE: Something on the node has been running a while, e.g. a looping component
        public bool IsBusy(Guid nodeId) =>
            _runningComponents.GetValueOrDefault(nodeId) > 0;

        public bool TryGetNode(Guid id, out NodeSnapshot node) =>
            _nodes.TryGetValue(id, out node!);

        public NodeSnapshot? NodeAt(Vector3D position) =>
            _nodes.Values.FirstOrDefault(x => x.Position == position);

        public bool HasActiveEffects =>
            _messages.Count > 0 || _flashes.Count > 0 || _soundingNotes.Count > 0;

        public void PruneEffects()
        {
            var now = Seconds;
            _flashes.RemoveAll(x => now - x.StartedAt > FLASH_SECONDS);
        }

        // NOTE: Where each node's emitting components will send to, for drawing the routes. Recomputed when nodes change
        public IReadOnlyList<Connection> Connections => _connections ??= BuildConnections();

        private List<Connection> BuildConnections()
        {
            var byPosition = _nodes.Values.ToDictionary(x => x.Position);
            var connections = new List<Connection>();

            foreach (var node in _nodes.Values)
            {
                var directions = new HashSet<Direction>();

                foreach (var (port, components) in node.Ports)
                {
                    foreach (var component in components)
                    {
                        if (component.Config is EmitConfig)
                            directions.Add(port);
                        else if (component.Config is RedirectConfig redirect)
                            directions.Add(redirect.To);
                    }
                }

                foreach (var direction in directions)
                {
                    var step = direction.ToVector();
                    var position = node.Position;
                    var hasTarget = false;

                    for (var distance = 1; distance <= SpatialPropagator.MAX_RAYCAST_DISTANCE; distance++)
                    {
                        position += step;

                        if (byPosition.ContainsKey(position))
                        {
                            hasTarget = true;
                            break;
                        }
                    }

                    connections.Add(new Connection(node.Id, node.Position, direction, hasTarget ? position : node.Position + step * 2, hasTarget));
                }
            }

            return connections;
        }

        private void NodesChanged()
        {
            _connections = null;
            NodesVersion++;
        }

        private static void Decrement(Dictionary<Guid, int> counts, Guid key)
        {
            var count = counts.GetValueOrDefault(key) - 1;

            if (count > 0)
                counts[key] = count;
            else
                counts.Remove(key);
        }
    }
}
