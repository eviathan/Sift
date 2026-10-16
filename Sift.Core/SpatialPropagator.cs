using Microsoft.Extensions.Logging;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Grid;
using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core
{
    public class SpatialPropagator
    {
        private const int MAX_RAYCAST_DISTANCE = 100;

        private readonly ILogger<SpatialPropagator> _logger;
        private readonly Grid<Node> _grid;

        public SpatialPropagator(ILogger<SpatialPropagator> logger, Grid<Node> grid)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }
        
        public bool TryRaycast(Vector3D position, Direction direction, long ticksPerCell, Time currentTime, out RaycastResult result)
        {
            var distance = 0;
            var currentPostiion = position;

            while(true)
            {
                currentPostiion = currentPostiion.Step(direction);
                distance++;
                
                if(_grid.TryGetNode(currentPostiion, out var node))
                {
                    var expectedListeningDirection = direction.Opposite();
                    var arrivalTicks = currentTime.Ticks + checked(distance * ticksPerCell);

                    result = new RaycastResult
                    {
                        NodeId = node.Id,
                        PortDirection = expectedListeningDirection,
                        Distance = distance,
                        ArrivalTicks = arrivalTicks
                    };

                    return true;
                }
                
                if(distance > MAX_RAYCAST_DISTANCE)
                    break;
            }

            result = default;
            return false;
        }
    }
}