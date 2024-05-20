namespace Sift.Sequencer.Models
{
    public class GridWalkerPool : Pool<GridWalker>
    {
        public GridWalkerPool(Func<Pool<GridWalker>, GridWalker> objectGenerator, int initialCapacity)
            : base(objectGenerator, initialCapacity)
        {
        }
    }
}