using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;
        
        // NOTES
        // - We will probably want to keep a cyclic directional tree for the traversal/ sequence 
        // - we will probably want to keep a dictionary to store the positional data of the nodes
        // - We will probably want to avoid GC so use a Pool for the TreeNodes
        public List<Tree> Trees { get; set; } = new List<Tree>();

        public Dictionary<(int x, int y), TreeNode> TreeNodeGrid = new Dictionary<(int x, int y), TreeNode>();
        
        public Sequence()
        {
            var node1 = new TreeNode {  };   
        }
    }
}