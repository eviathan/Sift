using System.Collections.Concurrent;

namespace Sift.Core.Models.Nodes
{
    // NOTE: Values shared by the components on one node for one playback, so a component on one port can affect one on
    // another (e.g. Toggle flips the node's gate, Gate reads it). Cleared when playback stops
    public sealed class NodeState
    {
        private readonly ConcurrentDictionary<string, object?> _values = new();

        public T Get<T>(string key, T fallback) =>
            _values.TryGetValue(key, out var value) && value is T typed ? typed : fallback;

        public void Set<T>(string key, T value) =>
            _values[key] = value;

        // NOTE: Atomic read-modify-write, components can run concurrently
        public T Update<T>(string key, T seed, Func<T, T> update) =>
            (T)_values.AddOrUpdate(key, _ => update(seed), (_, current) => update(current is T typed ? typed : seed))!;
    }
}
