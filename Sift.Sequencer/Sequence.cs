using Sift.Sequencer.Models;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;

        // NOTES
        // - We will probably want to keep a cyclic directional tree for the traversal/ sequence 
        // - we will probably want to keep a dictionary to store the positional data of the nodes
        // - We will probably want to avoid GC so use a Pool for the TreeNodes
        public List<Tree> Trees { get; set; } = [];
        public Grid<Node> Grid { get; set; } = new();
        public Pool<NodeWalker> Walkers { get; set; } = new(10)

        public void AddNode<TTreeNode>(int x, int y, int z)
            where TTreeNode : Node
        {
            // var treeNodeType = typeof(TTreeNode);
            // var newTreeNode = _treeNodePools[treeNodeType].TakeNode();

            // if (Grid.TryGetValue((x, y, 0), out var treeNode) && treeNode is Node)
            // {
            //     Grid.Add((x, y), newTreeNode);
            // }
            // else
            // {
            //     if (!_treeNodePools.ContainsKey(treeNodeType))
            //         _treeNodePools[treeNodeType] = new Pool<Node>();

            //     Grid.Add((x, y), newTreeNode);
            // }
        }

        public void RemoveNode<TTreeNode>(int x, int y)
            where TTreeNode : Node
        {
            // if (Grid.TryGetValue((x, y), out var treeNode))
            // {
            //     var treeNodeType = typeof(TTreeNode);
            //     _treeNodePools[treeNodeType].ReturnNode(treeNode);
            // }
        }

        public void ResetTrees()
        {
            foreach (var tree in Trees)
                tree.ResetTree();
        }
    }
}