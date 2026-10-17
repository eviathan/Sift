namespace Sift.Tests.Unit.Support
{
    // NOTE: For tests that need the real "Sift MIDI" virtual device, which other apps on the machine can also use.
    // Skipped unless SIFT_DEVICE_TESTS=1
    public sealed class DeviceFactAttribute : FactAttribute
    {
        public DeviceFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("SIFT_DEVICE_TESTS") != "1")
                Skip = "Uses the real MIDI device, set SIFT_DEVICE_TESTS=1 to run";
        }
    }

    // NOTE: For tests that measure the real-time clock, which a busy machine can disturb. Skipped when SIFT_SKIP_TIMING=1
    public sealed class TimingFactAttribute : FactAttribute
    {
        public TimingFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("SIFT_SKIP_TIMING") == "1")
                Skip = "Real-time clock tests skipped (SIFT_SKIP_TIMING=1)";
        }
    }
}
