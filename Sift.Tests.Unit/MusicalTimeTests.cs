using Sift.Core;
using Sift.Core.Commands;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    public class MusicalTimeTests
    {
        [Theory]
        [InlineData(NoteDuration.WholeNote, 384)]
        [InlineData(NoteDuration.QuarterNote, 96)]
        [InlineData(NoteDuration.SixteenthNote, 24)]
        [InlineData(NoteDuration.SixtyFourthNote, 6)]
        public void Note_durations_convert_to_ticks(NoteDuration duration, long ticks) =>
            Assert.Equal(ticks, duration.ToTicks());

        [Fact]
        public void A_looping_component_emits_on_exact_ticks()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(48));
            sift.Listener(new(1, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Play(600);

            var arrivals = sift.ArrivalsAt("Target");
            Assert.True(arrivals.Count >= 8);
            Assert.Equal(arrivals.Select((_, i) => 144L + 48 * i), arrivals);
        }

        [Fact]
        public void An_emit_for_a_tick_the_clock_has_passed_arrives_late_rather_than_being_lost()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(48, Once: true));
            sift.Listener(new(1, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(1000));
            Thread.Sleep(50);
            sift.Runtime.AdvanceTo(new Time(1010));

            Assert.Single(sift.ArrivalsAt("Target"));
        }

        [Fact]
        public void Removing_a_looping_component_cancels_it_and_leaves_others_running()
        {
            using var sift = new SiftFixture();
            var a = sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(24));
            var loop = a.GetPort(Direction.Right).Components.Single();
            sift.Source(new(0, 5, 0), Direction.Right, new TickPulseConfig(24));
            sift.Listener(new(1, 0, 0), Direction.Left, "A");
            sift.Listener(new(1, 5, 0), Direction.Left, "B");

            sift.Runtime.Start();
            sift.Play(200);
            Assert.Equal(2, sift.Counters.Running);

            _ = sift.Runtime.Submit(new RemoveComponent(a.Id, Direction.Right, loop.Id));
            sift.Play(400);

            Assert.Equal(1, sift.Counters.Running);
            Assert.True(sift.ArrivalsAt("A").Max() <= 320);
            Assert.True(sift.ArrivalsAt("B").Max() >= 380);

            sift.Runtime.Stop();
            SpinWait.SpinUntil(() => sift.Counters.Running == 0, 500);
            Assert.Equal(0, sift.Counters.Running);
        }

        [Fact]
        public void Removing_a_node_cancels_all_its_components()
        {
            using var sift = new SiftFixture();
            var node = sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(24));
            sift.Build(node, Direction.Up, new TickPulseConfig(24));

            sift.Runtime.Start();
            sift.Play(100);
            Assert.Equal(2, sift.Counters.Running);

            _ = sift.Runtime.Submit(new RemoveNode(node.Id));
            sift.Play(150);

            Assert.Equal(0, sift.Counters.Running);
        }

        [Fact]
        public void Waits_complete_on_their_tick_and_are_cancelled_by_stop()
        {
            using var sift = new SiftFixture();
            var context = (ISequencerContext)sift.Runtime;

            Assert.True(context.WaitTicksAsync(10, default).IsCanceled);

            sift.Runtime.Start();
            var pending = context.WaitTicksAsync(1000, default);
            sift.Runtime.AdvanceTo(new Time(10));
            sift.Runtime.Stop();
            SpinWait.SpinUntil(() => pending.IsCompleted, 500);
            Assert.True(pending.IsCanceled);

            sift.Runtime.Start();
            var wait = context.WaitUntilAsync(new Time(50), default);
            sift.Runtime.AdvanceTo(new Time(49));
            Assert.False(wait.IsCompleted);

            sift.Runtime.AdvanceTo(new Time(50));
            SpinWait.SpinUntil(() => wait.IsCompleted, 500);
            Assert.Equal(50, wait.Result.Ticks);
        }
    }
}
