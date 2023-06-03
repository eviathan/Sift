
namespace Sift.Sequencer.Grid
{
    public struct Position
    {
        // NOTE: If we support non grid snapping these will need to be updated to be doubles
        public int X { get; set; }
        public int Y { get; set; }

        public int Layer { get; set; }

        public Position(int x, int y, int layer = default)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public int Distance(Position to) 
        {
            // NOTE: If we move to allowing off grid this casting should be removed
            return (int) Math.Ceiling(Math.Sqrt(Math.Pow(to.X - X, 2) + Math.Pow(to.Y - Y, 2)));
        }
    }
}