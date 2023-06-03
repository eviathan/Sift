
namespace Sift.Sequencer.Grid
{
    public struct Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Layer { get; set; }

        public Position(int x, int y, int layer = default)
        {
            X = x;
            Y = y;
            Layer = layer;
        }
    }
}