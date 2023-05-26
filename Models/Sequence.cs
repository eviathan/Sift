using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 200;

        // NOTES
        // - We will probably want to keep a cyclic directional tree for the traversal/ sequence 
        // - we will probably want to keep a dictionary to store the positional data of the nodes
        // - We will probably want to avoid GC so use a Pool for the TreeNodes
        public List<Tree> Trees { get; set; } = new List<Tree>();

        public Dictionary<(int x, int y), ITreeNode> TreeNodeGrid = new Dictionary<(int x, int y), ITreeNode>();
        
        public Sequence()
        {
            // NOTE: This is just some test data
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
            Trees.Add(tree);
        }

        public void ResetTrees()
        {
            foreach (var tree in Trees)
                tree.ResetTree();
        }
    }
}