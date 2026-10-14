namespace Sift.Sequencer.Grid
{
    public struct Vector3D : IEquatable<Vector3D>
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

        public bool Equals(Vector3D other) =>
            X == other.X &&
            Y == other.Y &&
            Z == other.Z;

        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 31 + X.GetHashCode();
            hashCode = hashCode * 31 + Y.GetHashCode();
            hashCode = hashCode * 31 + Z.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) =>
            new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3D operator *(Vector3D a, int b) =>
            b != 1
                ? new(a.X * b, a.Y * b, a.Z * b)
                : a;
    }
}