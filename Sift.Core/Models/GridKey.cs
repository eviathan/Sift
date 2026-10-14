namespace Sift.Core.Models
{
    public readonly struct GridKey : IEquatable<GridKey>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GridKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public bool Equals(GridKey other) =>
            X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) =>
            obj is GridKey other && Equals(other);

        public static bool operator ==(GridKey left, GridKey right) =>
            left.Equals(right);
        
        public static bool operator !=(GridKey left, GridKey right) =>
            !left.Equals(right);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                hash = hash * 31 + Z;
                return hash;
            }
        }
    }
}