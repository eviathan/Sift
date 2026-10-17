using Sift.Core;
using Sift.Core.Commands;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    // NOTE: Messages travel a cell per TicksPerCell (96 at the default 1/4 grid) and behave physically when the grid
    // changes under them
    public class PropagationTests
    {
        [Fact]
        public void Message_arrives_after_its_distance_in_cells()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Listener(new(3, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(1000));

            Assert.Equal([288L], sift.ArrivalsAt("Target"));
        }

        [Fact]
        public void Node_placed_ahead_of_a_message_intercepts_it()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Listener(new(6, 0, 0), Direction.Left, "Far");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(100));
            _ = sift.Runtime.Submit(new AddNode(sift.Build(new Sift.Core.Nodes.Node(new(2, 0, 0)) { Name = "Blocker" }, Direction.Left, new LogConfig())));
            sift.Runtime.AdvanceTo(new Time(1000));

            Assert.Equal([192L], sift.ArrivalsAt("Blocker"));
            Assert.Empty(sift.ArrivalsAt("Far"));
        }

        [Fact]
        public void Node_placed_behind_a_message_is_passed_by()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Listener(new(6, 0, 0), Direction.Left, "Far");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(200));
            _ = sift.Runtime.Submit(new AddNode(sift.Build(new Sift.Core.Nodes.Node(new(1, 0, 0)) { Name = "Behind" }, Direction.Left, new LogConfig())));
            sift.Runtime.AdvanceTo(new Time(1000));

            Assert.Equal([576L], sift.ArrivalsAt("Far"));
            Assert.Empty(sift.ArrivalsAt("Behind"));
        }

        [Fact]
        public void Message_carries_on_when_its_target_moves_away()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            var near = sift.Listener(new(2, 0, 0), Direction.Left, "Near");
            sift.Listener(new(5, 0, 0), Direction.Left, "Next");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(100));
            _ = sift.Runtime.Submit(new MoveNode(near.Id, new(2, 1, 0)));
            sift.Runtime.AdvanceTo(new Time(1000));

            Assert.Equal([480L], sift.ArrivalsAt("Next"));
            Assert.Empty(sift.ArrivalsAt("Near"));
        }

        [Fact]
        public void Message_that_lost_its_target_can_still_be_caught_further_on()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            var target = sift.Listener(new(3, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(100));
            _ = sift.Runtime.Submit(new RemoveNode(target.Id));
            sift.Runtime.AdvanceTo(new Time(2000));

            Assert.Empty(sift.Arrivals);

            _ = sift.Runtime.Submit(new AddNode(sift.Build(new Sift.Core.Nodes.Node(new(30, 0, 0)) { Name = "Late" }, Direction.Left, new LogConfig())));
            sift.Runtime.AdvanceTo(new Time(3000));

            Assert.Equal([2880L], sift.ArrivalsAt("Late"));
        }

        [Fact]
        public void Message_that_hits_nothing_dissipates_at_the_maximum_distance()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(SpatialPropagator.MAX_RAYCAST_DISTANCE * 96 + 1));

            Assert.True(sift.Log.Contains($"[Tick {SpatialPropagator.MAX_RAYCAST_DISTANCE * 96}] Message 'play' dissipated"));
        }

        [Fact]
        public void Redirects_ping_pong_on_exact_ticks()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            var a = sift.Node(new(3, 0, 0), "A", Direction.Left, new LogConfig(), new RedirectConfig(Direction.Up));
            sift.Build(a, Direction.Up, new LogConfig(), new RedirectConfig(Direction.Up));
            sift.Node(new(3, 2, 0), "B", Direction.Down, new LogConfig(), new RedirectConfig(Direction.Down));

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(1000));

            Assert.Equal([288L, 672L], sift.ArrivalsAt("A"));
            Assert.Equal([480L, 864L], sift.ArrivalsAt("B"));
        }

        [Fact]
        public void Large_advances_jump_straight_to_the_next_event()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Listener(new(50, 0, 0), Direction.Left, "Far");

            sift.Runtime.Start();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            sift.Runtime.AdvanceTo(new Time(50_000_000));

            Assert.True(stopwatch.ElapsedMilliseconds < 200, $"took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Equal([4800L], sift.ArrivalsAt("Far"));
            Assert.Equal(50_000_000, sift.Runtime.Now.Ticks);
        }
    }
}
