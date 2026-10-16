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
        public const int MAX_RAYCAST_DISTANCE = 100;

        private readonly ILogger<SpatialPropagator> _logger;

        public SpatialPropagator(ILogger<SpatialPropagator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // NOTE: Takes the grid rather than owning it so it can also be used against snapshots (e.g. UI previews)
        // minDistance skips cells a message has already travelled through
        public bool TryRaycast(Grid<Node> grid, Vector3D position, Direction direction, long ticksPerCell, Time currentTime, out RaycastResult result, int minDistance = 1)
        {
            var currentPosition = position;

            for (var distance = 1; distance <= MAX_RAYCAST_DISTANCE; distance++)
            {
                currentPosition = currentPosition.Step(direction);

                if (distance >= minDistance && grid.TryGetNode(currentPosition, out var node))
                {
                    result = new RaycastResult
                    {
                        NodeId = node.Id,
                        PortDirection = direction.Opposite(),
                        Distance = distance,
                        ArrivalTicks = currentTime.Ticks + checked(distance * ticksPerCell)
                    };

                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}
