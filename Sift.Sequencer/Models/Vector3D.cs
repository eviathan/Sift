namespace Sift.Sequencer.Grid
{
    public struct Vector3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3D(int x = 0, int y = 0, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Modify(int? x = null, int? y = null, int? z = null) =>
            (X, Y, Z) = (x ?? X, y ?? Y, z ?? Z);

        public static Vector3D operator +(Vector3D a, Vector3D b) =>
            new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3D operator *(Vector3D a, int b) =>
            b != 1
                ? new Vector3D(a.X * b, a.Y * b, a.Z * b)
                : a;
    }
}