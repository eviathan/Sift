
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Models
{
    public class GridWalker : IDisposable
    {
        public Vector3D Position { get; private set; }

        private Vector3D _startingPosition { get; set; }
        private Vector3D _direction { get; set; }
        private readonly Pool<GridWalker> _pool;

        public GridWalker(Pool<GridWalker> pool)
        {
            _pool = pool;
        }

        public GridWalker(Vector3D direction, Vector3D startingPosition, Pool<GridWalker> pool)
        {
            _startingPosition = startingPosition;
            _direction = direction;
            _pool = pool;
        }

        public void Traverse(int scalar = 1)
        {
            Position += _direction * scalar;
        }

        public void Translate(int tick = 1)
        {
            Position += _startingPosition * tick;
        }

        public void Set(Vector3D direction, Vector3D startingPosition)
        {
            _startingPosition = startingPosition;
            _direction = direction;
            Position = startingPosition;
        }

        public void Reset()
        {
            Position = _startingPosition;
        }

        public void Dispose()
        {
            _pool.Return(this);
        }
    }
}