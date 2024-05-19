
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Models
{
    public class Pool<T> where T : new()
    {
        private Queue<T> _pool { get; set; } = [];

        public Pool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                _pool.Enqueue(new T());
            }
        }

        public T LeaseItemOrDefault()
        {
            throw new NotImplementedException();
        }

        public bool TryLeaseItems(out List<T> items, int amount = 1, bool createNewIfEmpty = false, Action<T> prepareItem = null)
        {
            items = new List<T>();

            return true;
        }
    }
}