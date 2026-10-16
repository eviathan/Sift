using Sift.Core.Enums;

namespace Sift.Core.Models.Nodes
{
    public class NodeEvent : Entity
    {
        public Time DueTime { get; set; }

        public Message Message { get; set; }

        public Guid TargetNodeId { get; set; }

        public Direction TargetPortDirection { get; set; }

        public long Order { get; set; }

        public NodeEvent(Guid id, Time dueTime, Message message, Guid targetNodeId, Direction targetPortDirection, long order)
        {
            Id = id;
            DueTime = dueTime;
            Message = message;
            TargetNodeId = targetNodeId;
            TargetPortDirection = targetPortDirection;
            Order = order;
        }
    }
}