using Sift.Models;

public static class SequenceExtensions
{
    public static Sequence Basic(this Sequence sequence)
    {
        var node6 = new MidiEventTreeNode 
        { 
            Event = new MIDIEvent { Pitch = 39 + (12 * 3) }
        };

        var node5 = new MidiEventTreeNode
        {
            Next = node6, 
            Event = new MIDIEvent { Pitch = 43 + (12 * 3) }
        };

        var node4 = new MidiEventTreeNode
        {
            Next = node5,
            Event = new MIDIEvent { Pitch = 44 + (12 * 3) }
        };

        var node3 = new MidiEventTreeNode
        {
            Next = node4,
            Event = new MIDIEvent { Pitch = 43 + (12 * 3) }
        };

        var node2 = new MidiEventTreeNode
        {
            Next = node3,
            Event = new MIDIEvent { Pitch = 39 + (12 * 3) }
        };

            var node1 = new MidiEventTreeNode 
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