using Sift.Core;
using Sift.Core.Commands;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Nodes;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    public class EventStreamTests
    {
        [Fact]
        public void Events_describe_a_retarget_in_order_with_contiguous_sequence_numbers()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Listener(new(3, 0, 0), Direction.Left, "A");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(100));
            _ = sift.Runtime.Submit(new AddNode(sift.Build(new Node(new(2, 0, 0)) { Name = "Blocker" }, Direction.Left, new LogConfig())));
            sift.Runtime.AdvanceTo(new Time(400));

            var all = events.ReadAll();
            var seen = all.Where(x => x is not HistoryChanged).ToList();

            Assert.IsType<NodeTriggeredByPlay>(seen[0]);
            Assert.IsType<ComponentStarted>(seen[1]);
            Assert.Equal(ComponentOutcome.Completed, Assert.IsType<ComponentFinished>(seen[2]).Outcome);

            var emitted = Assert.IsType<MessageEmitted>(seen[3]);
            Assert.Equal(new Vector3D(3, 0, 0), emitted.EndPosition);
            Assert.Equal(288, emitted.ArrivalTime.Ticks);

            Assert.Equal("Blocker", Assert.IsType<NodeAdded>(seen[4]).Node.Name);

            var retargeted = Assert.IsType<MessageRetargeted>(seen[5]);
            Assert.Equal(new Vector3D(2, 0, 0), retargeted.EndPosition);
            Assert.Equal(192, retargeted.ArrivalTime.Ticks);

            var delivered = Assert.IsType<MessageDelivered>(seen[6]);
            Assert.Equal(192, delivered.Tick.Ticks);
            Assert.Equal(Direction.Left, delivered.Port);

            Assert.All(all.Zip(all.Skip(1)), pair => Assert.Equal(pair.First.Sequence + 1, pair.Second.Sequence));
        }

        [Fact]
        public void A_message_that_hits_nothing_reports_where_it_dissipated()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe();
            sift.Emitter(new(0, 0, 0), Direction.Right);

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(10000));

            var dissipated = Assert.Single(events.ReadAll().OfType<MessageDissipated>());
            Assert.Equal(9600, dissipated.Tick.Ticks);
            Assert.Equal(new Vector3D(100, 0, 0), dissipated.Position);
        }

        [Fact]
        public void A_snapshot_plus_later_events_is_consistent()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Emitter(new(0, 1, 0), Direction.Right);
            sift.Listener(new(5, 0, 0), Direction.Left, "A");

            var stopped = sift.Runtime.GetSnapshotAsync();
            Assert.True(stopped.IsCompletedSuccessfully);
            Assert.Equal(3, stopped.Result.Nodes.Count);

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(50));
            var pending = sift.Runtime.GetSnapshotAsync();
            Assert.False(pending.IsCompleted);

            sift.Runtime.AdvanceTo(new Time(60));
            var snapshot = pending.Result;

            Assert.True(snapshot.IsPlaying);
            Assert.Equal(50, snapshot.Tick.Ticks);
            Assert.Equal(2, snapshot.InFlight.Count);
            Assert.Contains(snapshot.InFlight, x => x.TargetNodeId != null && x.ArrivalTime.Ticks == 480);
            Assert.Contains(snapshot.InFlight, x => x.TargetNodeId == null);

            var source = snapshot.Nodes.First(x => x.Position == new Vector3D(0, 0, 0));
            var component = Assert.Single(source.Ports[Direction.Right]);
            Assert.Equal("emit", component.Type);
            Assert.Equal(ComponentCategory.Source, component.Category);

            Assert.All(events.ReadAll(), x => Assert.True(x.Sequence <= snapshot.LastEventSequence));
            sift.Runtime.AdvanceTo(new Time(600));
            Assert.All(events.ReadAll(), x => Assert.True(x.Sequence > snapshot.LastEventSequence));
        }

        [Fact]
        public void Grid_changes_and_history_are_published_with_their_source()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe();
            var runtime = sift.Runtime;
            var node = sift.Build(new Node(new(0, 0, 0)) { Name = "N" }, Direction.Left, new LogConfig());
            var midi = sift.Components.Create(new MidiConfig());

            runtime.Submit(new AddNode(node)).Wait();
            runtime.Submit(new MoveNode(node.Id, new(1, 2, 3))).Wait();
            runtime.Submit(new AddComponent(node.Id, Direction.Up, midi)).Wait();
            runtime.Submit(new UpdateComponentConfig(node.Id, Direction.Up, midi.Id, new MidiConfig(Note: 72))).Wait();
            Assert.ThrowsAny<Exception>(() => runtime.Submit(new MoveNode(Guid.NewGuid(), new(0, 0, 0))).Wait());
            runtime.Undo().Wait();
            runtime.Submit(new RemoveNode(node.Id)).Wait();

            var all = events.ReadAll();
            Assert.Equal("N", Assert.Single(all.OfType<NodeAdded>()).Node.Name);

            var moved = Assert.Single(all.OfType<NodeMoved>());
            Assert.Equal(new Vector3D(0, 0, 0), moved.From);
            Assert.Equal(new Vector3D(1, 2, 3), moved.To);
            Assert.Equal(CommandSource.User, moved.Source);

            var changed = all.OfType<NodeChanged>().ToList();
            Assert.Equal(3, changed.Count);
            Assert.Equal(72, ((MidiConfig)changed[1].Node.Ports[Direction.Up][0].Config).Note);
            Assert.Equal(60, ((MidiConfig)changed[2].Node.Ports[Direction.Up][0].Config).Note);

            Assert.Single(all.OfType<CommandRejected>());
            Assert.Equal(node.Id, Assert.Single(all.OfType<NodeRemoved>()).NodeId);

            var history = all.OfType<HistoryChanged>().ToList();
            Assert.Equal(6, history.Count);
            Assert.True(history[4].CanRedo);
            Assert.False(history[5].CanRedo);
        }

        [Fact]
        public void Component_edits_are_marked_as_coming_from_a_component()
        {
            using var sift = new SiftFixture();
            var node = sift.Listener(new(9, 9, 9), Direction.Left, "C");
            using var events = sift.Events.Subscribe();

            sift.Runtime.Start();
            _ = ((ISequencerContext)sift.Runtime).Submit(new MoveNode(node.Id, new(8, 8, 8)));
            sift.Runtime.AdvanceTo(new Time(1));

            var all = events.ReadAll();
            Assert.Equal(CommandSource.Component, Assert.Single(all.OfType<NodeMoved>()).Source);
            Assert.Empty(all.OfType<HistoryChanged>());
        }

        [Fact]
        public void Component_lifecycle_and_notes_are_published()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe();
            var source = sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Build(source, Direction.Up, new FaultConfig());
            sift.Build(source, Direction.Left, new MidiConfig(Note: 50, Channel: 2, Duration: NoteDuration.EighthNote));

            sift.Runtime.Start();
            sift.Play(100);
            Thread.Sleep(50);
            sift.Runtime.Stop();
            Thread.Sleep(50);

            var all = events.ReadAll();
            var finished = all.OfType<ComponentFinished>().ToList();

            Assert.Equal(all.OfType<ComponentStarted>().Select(x => x.InvocationId).Order(), finished.Select(x => x.InvocationId).Order());
            Assert.Contains(finished, x => x.Outcome == ComponentOutcome.Faulted && x.Error == "boom");

            var on = Assert.Single(all.OfType<NoteOn>());
            Assert.Equal((source.Id, 2, 50), (on.NodeId, on.Channel, on.Note));
            Assert.Equal(0, on.Tick.Ticks);
            Assert.InRange(Assert.Single(all.OfType<NoteOff>()).Tick.Ticks, 48, 52);
        }

        [Fact]
        public void A_slow_subscriber_loses_its_oldest_events_and_is_told()
        {
            using var sift = new SiftFixture();
            using var events = sift.Events.Subscribe(capacity: 16);
            sift.Source(new(0, 0, 0), Direction.Right, new TickPulseConfig(4));
            sift.Listener(new(1, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Play(300);
            sift.Runtime.Stop();
            Thread.Sleep(50);

            var all = events.ReadAll();
            Assert.Contains(all, x => x is EventsDropped);
            Assert.True(all.Count <= 17);
        }
    }
}
