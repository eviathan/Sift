using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Sequencer.Nodes
{
    public interface INode
    {
        INode? Parent { get; set; }
        List<INode> Children { get; set; }

        void DidEnd();
        void DidStart();
    }
}