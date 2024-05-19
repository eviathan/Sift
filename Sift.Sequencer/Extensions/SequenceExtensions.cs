using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.Grid;
using Sift.Sequencer.MIDI;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Extensions
{
    public static class SequencerExtensions
    {
        public static Sequence Basic(this Sequence sequence)
        {
            sequence.AddNode(0, 0, 0, new TestNode(new GridVector(0, 0, 0)));

            sequence.AddNode(4, 0, 0, new TestNode(new GridVector(4, 0, 0)));

            sequence.AddNode(5, 0, 0, new TestNode(new GridVector(5, 0, 0)));

            sequence.AddNode(8, 0, 0, new TestNode(new GridVector(8, 0, 0)));

            return sequence;
        }

        // public static Sequence Basic(this Sequence sequence)
        // {
        //     var node6 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 75 },
        //         new Position(5, 0)
        //     );

        //     var node5 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 79 },
        //         new Position(4, 0),
        //         children: new List<Node> { node6 }            
        //     );

        //     var node4 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 80 },
        //         new Position(3, 0),
        //         children: new List<Node> { node5 }
        //     );

        //     var node3 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 79 },
        //         new Position(3, 0),
        //         children: new List<Node> { node4 }
        //     );

        //     var node2 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 75 },
        //         new Position(3, 0),
        //         children: new List<Node> { node3 }
        //     );

        //     var node1 = new MidiEventNode(
        //         new MIDIEvent { Pitch = 72 },
        //         new Position(3, 0),
        //         children: new List<Node> { node2 }
        //     );

        //     node6.Children.Add(node1);

        //     var tree = new Tree(node1);
        //     sequence.Trees.Add(tree);

        //     return sequence;
        // }
    }
}