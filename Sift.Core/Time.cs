namespace Sift.Core
{
    public readonly struct Time : IComparable<Time>
    {
        public readonly long Ticks;

        public Time(long ticks)
        {
            Ticks = ticks;
        }

        public int CompareTo(Time other) =>
            Ticks.CompareTo(other.Ticks);

        public static Time operator +(Time a, long ticks) =>
            new(a.Ticks + ticks);
    }
}