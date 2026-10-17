using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Music;
using Sift.Core.Nodes;

namespace Sift.App
{
    // NOTE: A small piece that shows the components off: a clock drives an arpeggio, every other beat climbs to a chord
    // that sometimes sends an octave-up bell down the right, which echoes up a layer where a node walks as it plays. A
    // slow pulse on the top layer drops a bass note straight down through the stack
    public static class DemoScene
    {
        public static void Build(Sequence sequence, ComponentFactory components)
        {
            sequence.Tempo = 112;
            sequence.Quantisation = new Quantisation(1, 16);

            Node Add(int x, int y, int z, string name, bool emitOnPlay = false)
            {
                var node = new Node(new Vector3D(x, y, z)) { Name = name, EmitOnPlay = emitOnPlay };
                sequence.Grid.AddNode(node);
                return node;
            }

            void On(Node node, Direction port, params IComponentConfig[] configs)
            {
                foreach (var config in configs)
                    node.GetPort(port).Add(components.Create(config));
            }

            // Layer 0
            On(Add(0, 0, 0, "Clock", emitOnPlay: true), Direction.Right, new ClockConfig(NoteDuration.QuarterNote));

            On(Add(4, 0, 0, "Arp"), Direction.Left,
                new NotesConfig("C4 E4 G4 B4 D5", NoteOrder.UpDown, 96, 1, NoteDuration.SixteenthNote),
                new DivideConfig(2, Direction.Up));

            On(Add(4, 4, 0, "Chords"), Direction.Down,
                new ChordConfig(48, ChordQuality.Major7, 1, 70, 1, NoteDuration.HalfNote),
                new ChanceConfig(60, Direction.Right));

            On(Add(8, 4, 0, "Octave"), Direction.Left, new TransposeConfig(12, Direction.Down));

            On(Add(8, 0, 0, "Bell"), Direction.Up,
                new MidiConfig(Note: 67, Velocity: 80, Duration: NoteDuration.EighthNote),
                new DelayConfig(NoteDuration.EighthNote, Direction.Forwards));

            // Layer 1
            On(Add(8, 0, 1, "Echo"), Direction.Backwards,
                new NotesConfig("G5 E5 C5", NoteOrder.Down, 70, 1, NoteDuration.SixteenthNote),
                new RedirectConfig(Direction.Left));

            On(Add(3, 0, 1, "Walker"), Direction.Right,
                new MoveConfig(Direction.Up, 1),
                new MidiConfig(Note: 72, Velocity: 60));

            // Layer 2
            On(Add(0, 4, 2, "Pulse", emitOnPlay: true), Direction.Backwards, new ClockConfig(NoteDuration.WholeNote));
            On(Add(0, 4, 0, "Bass"), Direction.Forwards, new MidiConfig(Note: 36, Duration: NoteDuration.QuarterNote));
        }
    }
}
