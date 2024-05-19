namespace Sift.Sequencer.Grid
{
    public struct GridVector
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public GridVector(int x = 0, int y = 0, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Modify(int? x = null, int? y = null, int? z = null) =>
            (X, Y, Z) = (x ?? X, y ?? Y, z ?? Z);
    }
}