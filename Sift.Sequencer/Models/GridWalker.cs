
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Models
{
    public class GridWalker
    {
        public GridVector Position { get; private set; }

        private GridVector _startingPosition { get; set; }
        private GridVector _direction { get; set; }

        public GridWalker() { }

        public GridWalker(GridVector direction, GridVector startingPosition)
        {
            _startingPosition = startingPosition;
            _direction = direction;
        }

        public void Traverse(int tick = 1)
        {
            Position += _direction * tick;
        }

        public void Translate(int tick = 1)
        {
            Position += _startingPosition * tick;
        }

        public void Set(GridVector direction, GridVector startingPosition)
        {
            _startingPosition = startingPosition;
            _direction = direction;
            Position = startingPosition;
        }

        public void Reset()
        {
            Position = _startingPosition;
        }
    }
}