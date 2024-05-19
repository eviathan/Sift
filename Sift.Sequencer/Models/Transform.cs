
namespace Sift.Sequencer.Grid
{
    public struct Position
    {
        // NOTE: If we support non grid snapping these will need to be updated to be doubles
        public int X { get; set; }
        public int Y { get; set; }

        public ushort Layer { get; set; }

        public Position(int x, int y, ushort layer = default)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public double Distance(Position to) 
        {
            return Math.Ceiling(Math.Sqrt(Math.Pow(to.X - X, 2) + Math.Pow(to.Y - Y, 2)));
        }
    }
}