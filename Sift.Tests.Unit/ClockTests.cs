using System.Diagnostics;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    // NOTE: These run the real clock thread, so they take a couple of seconds and a very busy machine can upset them
    public class ClockTests
    {
        [TimingFact]
        public void Events_land_within_a_few_milliseconds_of_their_tick()
        {
            using var sift = new SiftFixture();
            sift.Sequence.Tempo = 120;
            sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(24));
            sift.Node(new(1, 0, 0), "Stamp", Direction.Left, new StampConfig());
            var sequencer = sift.CreateSequencer();

            sift.Counters.Stopwatch.Restart();
            sequencer.Play();
            Thread.Sleep(1500);
            sequencer.Stop();

            // NOTE: 120 BPM at 96 ticks a quarter is 192 ticks a second
            var stamps = sift.Counters.Stamps.ToArray();
            var errors = stamps.Select(x => Math.Abs(x.Milliseconds - x.Tick / 192.0 * 1000)).ToArray();

            Assert.True(stamps.Length >= 7, $"{stamps.Length} stamps");
            Assert.Equal(stamps.Select((_, i) => 120L + 24 * i), stamps.Select(x => x.Tick));
            Assert.True(errors.Max() < 5, $"max error {errors.Max():F2}ms");
        }

        [TimingFact]
        public void Tempo_changes_change_speed_without_jumping_and_transport_is_published()
        {
            using var sift = new SiftFixture();
            sift.Sequence.Tempo = 120;
            using var events = sift.Events.Subscribe();
            var sequencer = sift.CreateSequencer();

            sequencer.Play();
            var samples = new List<(double Milliseconds, double Position)>();
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < 1000)
            {
                if (stopwatch.ElapsedMilliseconds >= 500 && sift.Sequence.Tempo != 240)
                    sift.Sequence.Tempo = 240;

                samples.Add((stopwatch.Elapsed.TotalMilliseconds, sequencer.Position));
                Thread.Sleep(5);
            }

            sequencer.Stop();

            var rates = samples.Zip(samples.Skip(1), (a, b) => (b.Position - a.Position) / (b.Milliseconds - a.Milliseconds) * 1000).ToArray();
            var before = rates.Take(rates.Length / 2 - 5).Average();
            var after = rates.Skip(rates.Length / 2 + 5).Average();

            Assert.All(rates, rate => Assert.True(rate >= 0));
            Assert.InRange(before, 172, 212);
            Assert.InRange(after, 344, 424);
            Assert.Equal(0, sequencer.Position);

            var transport = events.ReadAll().OfType<TransportChanged>().ToList();
            Assert.True(transport.First().IsPlaying);
            Assert.Contains(transport, x => x.IsPlaying && x.Tempo == 240);
            Assert.False(transport.Last().IsPlaying);
        }
    }
}
