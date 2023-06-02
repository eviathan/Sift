using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Tree
    {
        public INode RootNode { get; private set; }
        public Queue<INode> ActiveNodes { get; private set; } = new Queue<INode>();

        public Tree(INode rootNode)
        {
            RootNode = rootNode;
            ActiveNodes.Enqueue(rootNode);
        }

        public void Traverse()
        {
            var childNodes = new List<INode>();
            while(ActiveNodes.TryDequeue(out var activeNode)) 
            {
                activeNode.DidStart();
                activeNode.Parent?.DidEnd();
                childNodes.AddRange(activeNode.Children);
            }

            foreach (var childNode in childNodes)
            {
                ActiveNodes.Enqueue(childNode);                
            }     
        }

        public void ResetTree()
        {
            ActiveNodes = new Queue<INode>(new List<INode> { RootNode });
        }
    }
}