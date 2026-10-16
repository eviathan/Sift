using Sift.Core.Enums;

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
    }
}