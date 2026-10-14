
using System.Collections.Concurrent;

// TODO: Add method which tries to get the item and if there are non in the pool it extends the size of the pool
namespace Sift.Sequencer.Models
{
    public class Pool<T> where T : class, IDisposable
    {
        public int Size => _objects.Count;

        private readonly ConcurrentBag<T> _objects;
        private readonly Func<Pool<T>, T> _objectGenerator;

        public Pool(Func<Pool<T>, T> objectGenerator, int initialCapacity)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));

            for (int i = 0; i < initialCapacity; i++)
                _objects.Add(_objectGenerator(this));
        }

        public T Get()
        {
            if (_objects.TryTake(out var item))
                return item;

            return _objectGenerator(this);
        }

        public bool TryGet(out T item)
        {
            return _objects.TryTake(out item!);
        }

        public void Return(T item)
        {
            _objects.Add(item);
        }
    }
}