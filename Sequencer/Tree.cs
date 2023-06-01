using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Tree
    {
        public INode StartNode { get; private set; }
        public INode? PreviousNode { get; private set; }
        public INode CurrentNode { get; private set; }

        public Tree(INode startNode)
        {
            StartNode = startNode;
            CurrentNode = startNode;
        }

        public void Traverse()
        {
            if(CurrentNode == null)
                return;
            
            CurrentNode.DidStart();
            PreviousNode?.DidEnd();

            PreviousNode = CurrentNode;
            CurrentNode = CurrentNode.Next;
        }

        public void ResetTree()
        {
            CurrentNode = StartNode;
            PreviousNode = default;
        }
    }
}