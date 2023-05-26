using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Tree
    {
        public ITreeNode StartNode { get; private set; }
        public ITreeNode? PreviousNode { get; private set; }
        public ITreeNode CurrentNode { get; private set; }

        public Tree(ITreeNode startNode)
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