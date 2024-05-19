namespace Sift.Sequencer.Models
{
    public class Grid<TItem>
    {
        private Dictionary<(int x, int y, int z), TItem> _grid { get; set; } = [];

        public TItem this[int x, int y, int z]
        {
            get
            {
                if (_grid.TryGetValue((x, y, z), out var value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException($"No element found at ({x}, {y}, {z}).");
                }
            }
            set
            {
                _grid[(x, y, z)] = value;
            }
        }

        public bool TryGetValue(int x, int y, int z, out TItem value) => _grid.TryGetValue((x, y, z), out value);
        public bool Contains(int x, int y, int z) => _grid.ContainsKey((x, y, z));
        public void Remove(int x, int y, int z) => _grid.Remove((x, y, z));
        public void Clear() => _grid.Clear();
        public int Count => _grid.Count;
        public IEnumerable<(int x, int y, int z)> Keys => _grid.Keys;
        public IEnumerable<TItem> Values => _grid.Values;
    }
}