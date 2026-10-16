using Sift.Core.Enums;

namespace Sift.Core.Extensions
{
    public static class NoteDurationExtensions
    {
        // NOTE: NoteDuration values are in 256ths of a quarter note
        public static long ToTicks(this NoteDuration duration) =>
            (long)duration * Time.TicksPerQuarterNote / (long)NoteDuration.QuarterNote;
    }
}
