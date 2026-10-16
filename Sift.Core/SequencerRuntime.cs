using Microsoft.Extensions.Logging;
using Sift.Core.Enums;
using Sift.Core.Factories;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;
using Sift.Core.Nodes;

namespace Sift.Core
{
    public class SequencerRuntime
    {
        private readonly ILogger<SequencerRuntime> _logger;
        private readonly Grid<Node> _grid;
        private readonly EventScheduler _scheduler;
        private readonly SpatialPropagator _spatialPropagator;
        private readonly ComponentEventArgsFactory _componentEventArgFactory;

        private CancellationTokenSource _playbackCancellationoTokenSources;
        private readonly List<Task> _activeComponentTasks = new();
        private Time _currentTime = new(0);
        private long _ticksPerCell = 240;

        public SequencerRuntime(ILogger<SequencerRuntime> logger, Grid<Node> grid, EventScheduler scheduler, SpatialPropagator spatialPropagator, ComponentEventArgsFactory componentEventArgFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _spatialPropagator = spatialPropagator ?? throw new ArgumentNullException(nameof(spatialPropagator));
            _componentEventArgFactory = componentEventArgFactory ?? throw new ArgumentNullException(nameof(componentEventArgFactory));

            _playbackCancellationoTokenSources = new CancellationTokenSource();
        }

        public void EmitMessage(Guid sourceNodeId, Direction portDirection, Message message)
        {
            if(!_grid.TryGetNode(sourceNodeId, out var node))
                return;

            _logger.LogInformation($"[Tick {_currentTime.Ticks}] Node {sourceNodeId} emitting '{message.Payload}' via {portDirection}");

            if (_spatialPropagator.TryRaycast(node.Position, portDirection, _ticksPerCell, _currentTime, out var result))
            {
                _scheduler.Schedule(new Time(result.ArrivalTicks), message, result.NodeId, result.PortDirection);
                
                _logger.LogInformation($" -> Raycast hit Node {result.NodeId} at distance {result.Distance} cells. Scheduled arrival for Tick {result.ArrivalTicks}.");
            }
            else
            {
                _logger.LogInformation(" -> Raycast hit nothing in space. Message dissipated.");
            }
        }

        public async Task AdvanceTo(Time targetTime)
        {
            while (_currentTime.Ticks <= targetTime.Ticks)
            {
                while (_scheduler.TryDequeueDue(_currentTime, out var evt))
                {
                    await DispatchAsync(evt!);
                }

                if (_currentTime.Ticks == targetTime.Ticks)
                    break;

                _currentTime = new Time(_currentTime.Ticks + 1);
            }
        }

        private async Task DispatchAsync(NodeEvent nodeEvent)
        {
            _currentTime = nodeEvent.DueTime;

            if(!_grid.TryGetNode(nodeEvent.TargetNodeId, out var node))
                return;
                
            var port = node.GetPort(nodeEvent.TargetPortDirection);

            _logger.LogInformation($"[Tick {_currentTime.Ticks}] Dispatching '{nodeEvent.Message.Payload}' to Node {nodeEvent.TargetNodeId} Port {nodeEvent.TargetPortDirection}");

            foreach (var component in port.Components)
            {
                var eventArgs = _componentEventArgFactory.Create(node);

                var task = component.InvokeAsync(eventArgs, _playbackCancellationoTokenSources.Token);
                _activeComponentTasks.Add(task);
            }
        }
    }
}