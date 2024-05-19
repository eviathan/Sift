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

        public static GridVector operator +(GridVector a, GridVector b) =>
            new GridVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static GridVector operator *(GridVector a, int b) =>
            b != 1
                ? new GridVector(a.X * b, a.Y * b, a.Z * b)
                : a;
    }
}