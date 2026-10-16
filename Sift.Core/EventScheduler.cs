using Sift.Core.Enums;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Core
{
    public class EventScheduler
    {
        private readonly PriorityQueue<NodeEvent, (long Ticks, long Order)> _queue = new();
        private long _nextId;
        private long _nextOrder;

        public Guid Schedule(Time dueTime, Message message, Guid targetNodeId, Direction targetPortDirection)
        {
            var id = Guid.NewGuid();
            var order = ++_nextOrder;

            var nodeEvent = new NodeEvent(id, dueTime, message, targetNodeId, targetPortDirection, order);

            _queue.Enqueue(nodeEvent, (dueTime.Ticks, order));

            return id;
        }

        public bool TryDequeueDue(Time currentTime, out NodeEvent? nodeEvent)
        {
            if (_queue.TryPeek(out _, out var priority) && priority.Ticks <= currentTime.Ticks)
            {
                _queue.TryDequeue(out nodeEvent, out _);
                return true;
            }

            nodeEvent = null;
            return false;
        }
    }
}