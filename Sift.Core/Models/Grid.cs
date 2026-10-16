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
                var key = new Vector3D(x, y, z);

                if (_positionCache.TryGetValue(key, out var value))
                    return value;
                else
                    throw new KeyNotFoundException($"No element found at ({x}, {y}, {z}).");
            }
            set
            {
                _positionCache[new Vector3D(x, y, z)] = value;
            }
        }

        public bool TryGetNode(Guid id, out TNode value) =>
            _idCache.TryGetValue(id, out value!);

        public bool TryGetNode(int x, int y, int z, out TNode value) =>
            _positionCache.TryGetValue(new Vector3D(x, y, z), out value!);

        public bool TryGetNode(Vector3D position, out TNode value) =>
            _positionCache.TryGetValue(new Vector3D(position.X, position.Y, position.Z), out value!);

        public bool Contains(int x, int y, int z) =>
            _positionCache.ContainsKey(new Vector3D(x, y, z));

        public void AddNode(TNode node)
        {
            if (node == null)
                throw new ArgumentNullException("Cannot add a null node to the Grid!");

            this[node.Position.X, node.Position.Y, node.Position.Z] = node;
            _idCache[node.Id] = node;
        }

        public void Remove(int x, int y, int z)
        {
            var node = this[x, y, z];

            _idCache.Remove(node.Id);            
            _positionCache.Remove(node.Position);
        }

        public void Clear() => _positionCache.Clear();

        public int Count => _positionCache.Count;

        public IEnumerable<Vector3D> Keys => _positionCache.Keys;

        public IEnumerable<TNode> Values => _positionCache.Values;
    }
}