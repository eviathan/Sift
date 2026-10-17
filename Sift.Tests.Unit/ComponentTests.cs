using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Grid;
using Sift.Core.Music;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    // NOTE: Sources start at (0, 0, 0) emitting right, a cell is 96 ticks, so the first arrival one cell on is at 96
    public class ComponentTests
    {
        [Fact]
        public void Clock_sends_repeats_a_beat_apart()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 3));
            sift.Listener(new(1, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Play(600, 2);

            Assert.Equal([96L, 192L, 288L], sift.ArrivalsAt("Target"));
        }

        [Fact]
        public void Divide_lets_every_nth_through_straight_on()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 4));
            sift.Node(new(1, 0, 0), "Divide", Direction.Left, new DivideConfig(2));
            sift.Listener(new(2, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Play(700, 2);

            Assert.Equal([192L, 384L], sift.ArrivalsAt("Target"));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 3)]
        public void Chance_lets_through_its_percentage(int percent, int expected)
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 3));
            sift.Node(new(1, 0, 0), "Chance", Direction.Left, new ChanceConfig(percent));
            sift.Listener(new(2, 0, 0), Direction.Left, "Target");

            sift.Runtime.Start();
            sift.Play(700, 2);

            Assert.Equal(expected, sift.ArrivalsAt("Target").Count);
        }

        [Fact]
        public void Delay_holds_then_sends_on()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Node(new(1, 0, 0), "Delay", Direction.Left, new DelayConfig(NoteDuration.EighthNote, Direction.Up));
            sift.Listener(new(1, 1, 0), Direction.Down, "Target");

            sift.Runtime.Start();
            sift.Play(400, 2);

            Assert.Equal([96L + 48 + 96], sift.ArrivalsAt("Target"));
        }

        [Fact]
        public void Split_copies_out_of_every_side_but_the_one_it_came_in()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Node(new(1, 0, 0), "Split", Direction.Left, new SplitConfig());
            sift.Listener(new(1, 1, 0), Direction.Down, "Up");
            sift.Listener(new(2, 0, 0), Direction.Left, "Right");
            sift.Listener(new(1, -1, 0), Direction.Up, "Down");

            sift.Runtime.Start();
            sift.Play(400, 2);

            Assert.Single(sift.ArrivalsAt("Up"));
            Assert.Single(sift.ArrivalsAt("Right"));
            Assert.Single(sift.ArrivalsAt("Down"));
        }

        [Fact]
        public void Rotate_sends_successive_messages_round_the_sides()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.WholeNote, 3));
            sift.Node(new(1, 0, 0), "Rotate", Direction.Left, new RotateConfig());
            sift.Listener(new(1, 1, 0), Direction.Down, "Up");
            sift.Listener(new(2, 0, 0), Direction.Left, "Right");
            sift.Listener(new(1, -1, 0), Direction.Up, "Down");

            sift.Runtime.Start();
            sift.Play(1300, 4);

            var order = sift.Arrivals.Select(x => x.Split(']')[1].Trim().Split(' ')[0]).ToList();
            Assert.Equal(["Up", "Right", "Down"], order);
        }

        [Fact]
        public void Closed_gate_blocks_until_a_toggle_opens_it()
        {
            using var closed = new SiftFixture();
            closed.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 2));
            closed.Node(new(1, 0, 0), "Gate", Direction.Left, new GateConfig(StartOpen: false));
            closed.Listener(new(2, 0, 0), Direction.Left, "Target");

            closed.Runtime.Start();
            closed.Play(500, 2);
            Assert.Empty(closed.ArrivalsAt("Target"));

            // NOTE: The toggle's message (from above) lands a beat before the clock's first one
            using var toggled = new SiftFixture();
            toggled.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 2));
            var gate = toggled.Node(new(2, 0, 0), "Gate", Direction.Left, new GateConfig(StartOpen: false));
            toggled.Build(gate, Direction.Up, new ToggleConfig(StartOpen: false));
            toggled.Emitter(new(2, 1, 0), Direction.Down);
            toggled.Listener(new(3, 0, 0), Direction.Left, "Target");

            toggled.Runtime.Start();
            toggled.Play(600, 2);
            Assert.Equal(2, toggled.ArrivalsAt("Target").Count);
        }

        [Fact]
        public void Transpose_shifts_notes_played_further_on()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Node(new(1, 0, 0), "Transpose", Direction.Left, new TransposeConfig(12));
            sift.Node(new(2, 0, 0), "Note", Direction.Left, new MidiConfig(Note: 60));

            sift.Runtime.Start();
            sift.Play(300, 2);
            Thread.Sleep(20);

            Assert.Contains(sift.Midi.Events, x => x.StartsWith("on ch1 n72"));
        }

        [Fact]
        public void Notes_plays_the_next_note_each_time()
        {
            using var sift = new SiftFixture();
            sift.Source(new(0, 0, 0), Direction.Right, new ClockConfig(NoteDuration.QuarterNote, 3));
            sift.Node(new(1, 0, 0), "Notes", Direction.Left, new NotesConfig("C4 E4 G4"));

            sift.Runtime.Start();
            sift.Play(500, 2);
            Thread.Sleep(20);

            var notes = sift.Midi.Events.Where(x => x.StartsWith("on")).Select(x => x.Split(' ')[2]);
            Assert.Equal(["n60", "n64", "n67"], notes);
        }

        [Theory]
        [InlineData(NoteOrder.Up, new[] { 0, 1, 2, 0 })]
        [InlineData(NoteOrder.Down, new[] { 2, 1, 0, 2 })]
        [InlineData(NoteOrder.UpDown, new[] { 0, 1, 2, 1, 0, 1 })]
        public void Notes_orders(NoteOrder order, int[] indexes)
        {
            int[] notes = [10, 11, 12];

            Assert.Equal(indexes.Select(x => notes[x]), indexes.Select((_, step) => NotesComponent.Pick(notes, step, order)));
        }

        [Fact]
        public void Chords_and_note_names()
        {
            Assert.Equal([64, 67, 72], new ChordConfig(60, ChordQuality.Major, 1).Notes().Order());
            Assert.Equal([58, 42, 72], NoteNames.ParseList("Bb3, F#2 72"));
            Assert.Equal("C4", NoteNames.Name(60));
            Assert.Equal("A#-1", NoteNames.Name(10));

            using var sift = new SiftFixture();
            Assert.ThrowsAny<ArgumentException>(() => sift.Components.Create(new NotesConfig("C4 H9")));
        }

        [Fact]
        public void Move_moves_while_playing_and_is_undone_on_stop()
        {
            using var sift = new SiftFixture();
            var node = sift.Source(new(0, 0, 0), Direction.Right, new MoveConfig(Direction.Up, 2));
            using var events = sift.Events.Subscribe();

            sift.Runtime.Start();
            sift.Play(20, 2);
            Assert.Equal(new Vector3D(0, 2, 0), node.Position);

            sift.Runtime.Stop();
            Assert.Equal(new Vector3D(0, 0, 0), node.Position);
            Assert.False(sift.Runtime.Undo().Result);
            Assert.Equal(2, events.ReadAll().OfType<NodeMoved>().Count());
        }
    }
}
