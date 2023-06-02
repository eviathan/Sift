using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Sequencer.Nodes
{
    public class PathTreeNode : INode
    {
        public INode? Parent { get; set; }
        public List<INode> Children { get; set; }

        public void DidStart() { }

        public void DidEnd() { }
    }
}