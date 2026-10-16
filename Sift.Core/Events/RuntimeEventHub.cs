using System.Threading.Channels;

namespace Sift.Core.Events
{
    // NOTE: Fans runtime events out to subscribers (UI, logging, tests). Publishing never blocks, a subscriber that
    // falls behind loses its oldest events and is sent EventsDropped so it can resync from a snapshot
    public class RuntimeEventHub
    {
        public const int DEFAULT_CAPACITY = 4096;

        private readonly object _sync = new();
        private readonly List<RuntimeEventSubscription> _subscriptions = [];
        private long _sequence;
        private volatile int _subscriberCount;

        public bool HasSubscribers => _subscriberCount > 0;

        public long LastSequence => Interlocked.Read(ref _sequence);

        public RuntimeEventSubscription Subscribe(int capacity = DEFAULT_CAPACITY)
        {
            lock (_sync)
            {
                var subscription = new RuntimeEventSubscription(this, capacity);
                _subscriptions.Add(subscription);
                _subscriberCount = _subscriptions.Count;

                return subscription;
            }
        }

        public void Publish(RuntimeEvent runtimeEvent, Time tick)
        {
            lock (_sync)
            {
                var sequence = Interlocked.Increment(ref _sequence);

                if (_subscriptions.Count == 0)
                    return;

                var sequenced = runtimeEvent with { Sequence = sequence, Tick = tick };

                foreach (var subscription in _subscriptions)
                    subscription.Write(sequenced);
            }
        }

        internal void Unsubscribe(RuntimeEventSubscription subscription)
        {
            lock (_sync)
            {
                _subscriptions.Remove(subscription);
                _subscriberCount = _subscriptions.Count;
            }
        }
    }

    public sealed class RuntimeEventSubscription : IDisposable
    {
        private readonly RuntimeEventHub _hub;
        private readonly Channel<RuntimeEvent> _channel;
        private long _dropped;

        public ChannelReader<RuntimeEvent> Reader => _channel.Reader;

        internal RuntimeEventSubscription(RuntimeEventHub hub, int capacity)
        {
            _hub = hub;
            _channel = Channel.CreateBounded<RuntimeEvent>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = true
                },
                _ => Interlocked.Increment(ref _dropped));
        }

        // NOTE: Only called under the hub's lock
        internal void Write(RuntimeEvent runtimeEvent)
        {
            var dropped = Interlocked.Exchange(ref _dropped, 0);

            if (dropped > 0)
                _channel.Writer.TryWrite(new EventsDropped(dropped) { Sequence = runtimeEvent.Sequence, Tick = runtimeEvent.Tick });

            _channel.Writer.TryWrite(runtimeEvent);
        }

        public void Dispose()
        {
            _hub.Unsubscribe(this);
            _channel.Writer.TryComplete();
        }
    }
}
