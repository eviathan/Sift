using Sift.Core.Enums;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Core
{
    public class EventScheduler
    {
        private readonly PriorityQueue<NodeEvent, (long Ticks, long Order)> _queue = new();
        private long _nextOrder;

        public int Count => _queue.Count;

        public Guid Schedule(Time dueTime, Message message, Guid? targetNodeId, Direction? targetPortDirection, Guid? inFlightId = null, int inFlightVersion = 0)
        {
            var id = Guid.NewGuid();
            var order = ++_nextOrder;

            var nodeEvent = new NodeEvent(id, dueTime, message, targetNodeId, targetPortDirection, order, inFlightId, inFlightVersion);

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

        public bool TryPeekDueTime(out Time dueTime)
        {
            if (_queue.TryPeek(out _, out var priority))
            {
                dueTime = new Time(priority.Ticks);
                return true;
            }

            dueTime = default;
            return false;
        }

        public void Clear() =>
            _queue.Clear();
    }
}
