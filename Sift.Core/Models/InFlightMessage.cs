using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Grid;

namespace Sift.Core.Models
{
    // NOTE: A message travelling along a ray. Its target is re-resolved whenever the grid changes, so a node placed
    // in its path intercepts it and it carries on past a node that moves out of the way
    public class InFlightMessage : Entity
    {
        public Guid SourceNodeId { get; }
        public Vector3D Origin { get; }
        public Direction Direction { get; }
        public Time EmitTime { get; }
        public long TicksPerCell { get; }
        public Message Message { get; }

        // NOTE: Null when the ray hits nothing, ArrivalTime is then when it dissipates
        public Guid? TargetNodeId { get; internal set; }
        public int Distance { get; internal set; }
        public Time ArrivalTime { get; internal set; }

        // NOTE: Bumped on every retarget so superseded arrivals in the scheduler can be ignored
        public int Version { get; internal set; }

        public Direction TargetPortDirection => Direction.Opposite();

        // NOTE: The target's cell, or where the ray gives up when there is no target
        public Vector3D EndPosition => Origin + Direction.ToVector() * Distance;

        public InFlightMessage(Guid sourceNodeId, Vector3D origin, Direction direction, Time emitTime, long ticksPerCell, Message message)
        {
            Id = Guid.NewGuid();
            SourceNodeId = sourceNodeId;
            Origin = origin;
            Direction = direction;
            EmitTime = emitTime;
            TicksPerCell = ticksPerCell;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
