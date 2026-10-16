using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.App
{
    // TODO: Replace with loading a .sift.json once serialisation lands
    public static class DemoScene
    {
        // A one-bar loop around a square on layer 0 that also sends a branch up through layer 1 and back down, plus an
        // emitter on layer 2 firing straight down through the stack
        public static void Build(Sequence sequence, ComponentFactory components)
        {
            sequence.Tempo = 120;
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

            // Layer 0: the loop
            var start = Add(0, 0, 0, "Start", emitOnPlay: true);
            On(start, Direction.Right, new EmitConfig());
            On(start, Direction.Up, new MidiConfig(Note: 72), new RedirectConfig(Direction.Right));

            var east = Add(4, 0, 0, "East");
            On(east, Direction.Left, new MidiConfig(Note: 60), new RedirectConfig(Direction.Up), new RedirectConfig(Direction.Forwards));

            var north = Add(4, 4, 0, "North");
            On(north, Direction.Down, new MidiConfig(Note: 64), new RedirectConfig(Direction.Left));

            var west = Add(0, 4, 0, "West");
            On(west, Direction.Right, new MidiConfig(Note: 67), new RedirectConfig(Direction.Down));

            // Layer 1: the branch
            var lift = Add(4, 0, 1, "Lift");
            On(lift, Direction.Backwards, new MidiConfig(Note: 48), new RedirectConfig(Direction.Right));

            var turn = Add(7, 0, 1, "Turn");
            On(turn, Direction.Left, new MidiConfig(Note: 55), new RedirectConfig(Direction.Up));

            var drop = Add(7, 2, 1, "Drop");
            On(drop, Direction.Down, new RedirectConfig(Direction.Backwards));

            var landing = Add(7, 2, 0, "Landing");
            On(landing, Direction.Forwards, new MidiConfig(Note: 79));

            // Layer 2: straight down through the stack
            var high = Add(2, 2, 2, "High", emitOnPlay: true);
            On(high, Direction.Backwards, new EmitConfig());

            var low = Add(2, 2, 0, "Low");
            On(low, Direction.Forwards, new MidiConfig(Note: 84, Duration: NoteDuration.EighthNote));
        }
    }
}
