using Sift.Sequencer.Grid;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Tree
    {
        public Node RootNode { get; private set; }
        public Queue<Node> ActiveNodes { get; private set; } = new Queue<Node>();

        public Tree(Node rootNode)
        {
            RootNode = rootNode;
            ActiveNodes.Enqueue(rootNode);
        }

        public void Traverse()
        {
            while (ActiveNodes.TryDequeue(out var activeNode))
            {
                // activeNode.Parent?.DidEnd();
                // activeNode.DidStart();
                // childNodes.AddRange(activeNode.Children);
            }
        }

        public void ResetTree()
        {
            ActiveNodes = new Queue<Node>(new List<Node> { RootNode });
        }
    }
}