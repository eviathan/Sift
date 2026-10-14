using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Models
{
    public class Grid<TItem>
    {
        private Dictionary<GridKey, TItem> _grid { get; set; } = [];

        public TItem this[int x, int y, int z]
        {
            get
            {
                var key = new GridKey(x, y, z);

                if (_grid.TryGetValue(key, out var value))
                    return value;
                else
                    throw new KeyNotFoundException($"No element found at ({x}, {y}, {z}).");
            }
            set
            {
                _grid[new GridKey(x, y, z)] = value;
            }
        }

        public bool TryGetValue(int x, int y, int z, out TItem value) =>
            _grid.TryGetValue(new GridKey(x, y, z), out value!);

        public bool TryGetNode(Vector3D position, out TItem value) =>
            _grid.TryGetValue(new GridKey(position.X, position.Y, position.Z), out value!);

        public bool Contains(int x, int y, int z) =>
            _grid.ContainsKey(new GridKey(x, y, z));

        public void Remove(int x, int y, int z) =>
            _grid.Remove(new GridKey(x, y, z));

        public void Clear() => _grid.Clear();

        public int Count => _grid.Count;

        public IEnumerable<GridKey> Keys => _grid.Keys;

        public IEnumerable<TItem> Values => _grid.Values;
    }
}