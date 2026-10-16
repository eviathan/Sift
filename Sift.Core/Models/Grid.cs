using Sift.Core.Grid;
using Sift.Core.Nodes;

namespace Sift.Core.Models
{
    public class Grid<TNode>
        where TNode : Node
    {
        private Dictionary<Vector3D, TNode> _positionCache { get; set; } = [];
        private Dictionary<Guid, TNode> _idCache { get; set; } = [];

        public TNode this[int x, int y, int z]
        {
            get
            {
                if (_positionCache.TryGetValue(new Vector3D(x, y, z), out var value))
                    return value;
                else
                    throw new KeyNotFoundException($"No element found at ({x}, {y}, {z}).");
            }
        }

        public TNode GetNode(Guid id) =>
            _idCache.TryGetValue(id, out var node)
                ? node
                : throw new KeyNotFoundException($"No node found with id {id}.");

        public bool TryGetNode(Guid id, out TNode value) =>
            _idCache.TryGetValue(id, out value!);

        public bool TryGetNode(int x, int y, int z, out TNode value) =>
            _positionCache.TryGetValue(new Vector3D(x, y, z), out value!);

        public bool TryGetNode(Vector3D position, out TNode value) =>
            _positionCache.TryGetValue(position, out value!);

        public bool Contains(int x, int y, int z) =>
            _positionCache.ContainsKey(new Vector3D(x, y, z));

        public void AddNode(TNode node)
        {
            if (node == null)
                throw new ArgumentNullException("Cannot add a null node to the Grid!");

            if (_positionCache.ContainsKey(node.Position))
                throw new InvalidOperationException($"A node already exists at {node.Position}.");

            if (_idCache.ContainsKey(node.Id))
                throw new InvalidOperationException($"A node with id {node.Id} already exists.");

            _positionCache[node.Position] = node;
            _idCache[node.Id] = node;
        }

        public void Remove(int x, int y, int z)
        {
            var node = this[x, y, z];

            _idCache.Remove(node.Id);
            _positionCache.Remove(node.Position);
        }

        public bool Remove(Guid id, out TNode node)
        {
            if (!_idCache.Remove(id, out node!))
                return false;

            _positionCache.Remove(node.Position);
            return true;
        }

        public void Move(Guid id, Vector3D position)
        {
            var node = GetNode(id);

            if (node.Position == position)
                return;

            if (_positionCache.ContainsKey(position))
                throw new InvalidOperationException($"A node already exists at {position}.");

            _positionCache.Remove(node.Position);
            node.Position = position;
            _positionCache[position] = node;
        }

        public void Clear()
        {
            _positionCache.Clear();
            _idCache.Clear();
        }

        public int Count => _positionCache.Count;

        public IEnumerable<Vector3D> Keys => _positionCache.Keys;

        public IEnumerable<TNode> Values => _positionCache.Values;
    }
}
