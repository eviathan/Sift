using Sift.Sequencer;
using Sift.Sequencer.MIDI;
using Sift.Sequencer.Nodes;

public static class SequenceExtensions
{
    public static Sequence Basic(this Sequence sequence)
    {
        var node6 = new MidiEventNode(new MIDIEvent { Pitch = 39 + (12 * 3) });
        var node5 = new MidiEventNode(new MIDIEvent { Pitch = 43 + (12 * 3) }, new List<INode> { node6 });
        var node4 = new MidiEventNode(new MIDIEvent { Pitch = 44 + (12 * 3) }, new List<INode> { node5 });
        var node3 = new MidiEventNode(new MIDIEvent { Pitch = 43 + (12 * 3) }, new List<INode> { node4 });
        var node2 = new MidiEventNode(new MIDIEvent { Pitch = 39 + (12 * 3) }, new List<INode> { node3 });
        var node1 = new MidiEventNode(new MIDIEvent { Pitch = 36 + (12 * 3) }, new List<INode> { node2 });

        node6.Children.Add(node1);

        var tree = new Tree(node1);
        sequence.Trees.Add(tree);

        return sequence;
    }
}