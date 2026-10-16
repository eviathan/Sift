using Sift.Core.Enums;

namespace Sift.Core.Models.Nodes
{
    public class NodeEvent : Entity
    {
        public Time DueTime { get; set; }

        public Message Message { get; set; }

        // NOTE: Null when an in-flight message dissipates without hitting anything
        public Guid? TargetNodeId { get; set; }

        // NOTE: Null triggers every port on the node (play), otherwise only the port the message arrived on
        public Direction? TargetPortDirection { get; set; }

        // NOTE: Set for in-flight message arrivals, the event is stale if the message has since been retargeted
        public Guid? InFlightId { get; set; }

        public int InFlightVersion { get; set; }

        public long Order { get; set; }

        public NodeEvent(Guid id, Time dueTime, Message message, Guid? targetNodeId, Direction? targetPortDirection, long order, Guid? inFlightId = null, int inFlightVersion = 0)
        {
            Id = id;
            DueTime = dueTime;
            Message = message;
            TargetNodeId = targetNodeId;
            TargetPortDirection = targetPortDirection;
            Order = order;
            InFlightId = inFlightId;
            InFlightVersion = inFlightVersion;
        }
    }
}
