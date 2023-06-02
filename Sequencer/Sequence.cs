using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public List<Tree> Trees { get; set; } = new List<Tree>();

        // TODO: WE WILL PROBABLLY NEED A GRID FOR EACH TYPE OF TREE NODE
        public Dictionary<(int x, int y), INode> TreeNodeGrid = new Dictionary<(int x, int y), INode>();

        private Dictionary<Type, Pool<INode>> _treeNodePools { get; set; } = new Dictionary<Type, Pool<INode>>();

        public void AddNode<TTreeNode>(int x, int y)
            where TTreeNode : INode
        {
            var treeNodeType = typeof(TTreeNode);
            var newTreeNode = _treeNodePools[treeNodeType].TakeNode();

            if(TreeNodeGrid.TryGetValue((x, y), out var treeNode) && treeNode is PathTreeNode)
            {
                TreeNodeGrid.Add((x, y), newTreeNode);
            }
            else 
            {
                if(!_treeNodePools.ContainsKey(treeNodeType))
                    _treeNodePools[treeNodeType] = new Pool<INode>();

                TreeNodeGrid.Add((x, y), newTreeNode);
            }
        }

        public void RemoveNode<TTreeNode>(int x, int y)
            where TTreeNode : INode 
        {
            if(TreeNodeGrid.TryGetValue((x, y), out var treeNode))
            {
                var treeNodeType = typeof(TTreeNode);
                _treeNodePools[treeNodeType].ReturnNode(treeNode);
            }

        }

        public void ResetTrees()
        {
            foreach (var tree in Trees)
                tree.ResetTree();
        }
    }
}