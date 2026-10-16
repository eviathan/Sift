using Sift.Core.Enums;
using Sift.Core.Grid;

namespace Sift.Core.Extensions
{
    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction direction) => direction switch
        {
            Direction.Backwards => Direction.Forwards,
            Direction.Forwards => Direction.Backwards,
            Direction.Up => Direction.Down,
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => throw new ArgumentOutOfRangeException("Could not find direction!")
        };

        // NOTE: Y is up, X is right, Z is forwards
        public static Vector3D ToVector(this Direction direction) => direction switch
        {
            Direction.Up => new Vector3D(0, 1, 0),
            Direction.Down => new Vector3D(0, -1, 0),
            Direction.Right => new Vector3D(1, 0, 0),
            Direction.Left => new Vector3D(-1, 0, 0),
            Direction.Forwards => new Vector3D(0, 0, 1),
            Direction.Backwards => new Vector3D(0, 0, -1),
            _ => throw new ArgumentOutOfRangeException("Could not find direction!")
        };
    }
}
