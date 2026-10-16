namespace Sift.Core
{
    public readonly struct Time : IComparable<Time>
    {
        public const int TicksPerQuarterNote = 96;

        public static readonly Time Zero = new(0);

        public readonly long Ticks;

        public Time(long ticks)
        {
            Ticks = ticks;
        }

        public int CompareTo(Time other) =>
            Ticks.CompareTo(other.Ticks);

        public override string ToString() =>
            Ticks.ToString();

        public static Time operator +(Time a, long ticks) =>
            new(a.Ticks + ticks);
    }
}
