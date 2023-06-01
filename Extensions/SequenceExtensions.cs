using Sift.Models;
using Sift.Sequencer;
using Sift.Sequencer.MIDI;
using Sift.Sequencer.Nodes;

public static class SequenceExtensions
{
    public static Sequence Basic(this Sequence sequence)
    {
        var node6 = new MidiEventNode 
        { 
            Event = new MIDIEvent { Pitch = 39 + (12 * 3) }
        };

        var node5 = new MidiEventNode
        {
            Next = node6, 
            Event = new MIDIEvent { Pitch = 43 + (12 * 3) }
        };

        var node4 = new MidiEventNode
        {
            Next = node5,
            Event = new MIDIEvent { Pitch = 44 + (12 * 3) }
        };

        var node3 = new MidiEventNode
        {
            Next = node4,
            Event = new MIDIEvent { Pitch = 43 + (12 * 3) }
        };

        var node2 = new MidiEventNode
        {
            Next = node3,
            Event = new MIDIEvent { Pitch = 39 + (12 * 3) }
        };

            var node1 = new MidiEventNode 
        { 
            Next = node2,
            Event = new MIDIEvent { Pitch = 36 + (12 * 3) }
        };

        node6.Next = node1;

        var tree = new Tree(node1);
        sequence.Trees.Add(tree);

        return sequence;
    }
}