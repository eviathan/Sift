using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Sequence
    {
        public ushort Tempo { get; private set; } = 120;

        // NOTES:
        // Maybe I can do the beat divisions like ableton does. i.e. as floats where 1 is equal to a full bar of 4/4 and everything else is relaitve to it. i.e. 1 crotchet beat then would be 0.25, a qaiver would be would be equal to 0.0625 and a bar of 16/17 would be 1.0625
        public double BeatDivision { get; private set; } = 0.25; // Crotchets

        // NOTES
        // - We will probably want to keep a cyclic directional tree for the traversal/ sequence 
        // - we will probably want to keep a dictionary to store the positional data of the nodes
        // - We will probably want to avoid GC so use a Pool for the TreeNodes 
        public List<Tree> Trees { get; set; } = new List<Tree>();

        // TODO: WE WILL PROBABLLY NEED A GRID FOR EACH TYPE OF TREE NODE
        public Dictionary<(int x, int y), ITreeNode> TreeNodeGrid = new Dictionary<(int x, int y), ITreeNode>();

        private Dictionary<Type, Pool<ITreeNode>> _treeNodePools { get; set; } = new Dictionary<Type, Pool<ITreeNode>>();

        public void AddNode<TTreeNode>(int x, int y)
            where TTreeNode : ITreeNode
        {
            var treeNodeType = typeof(TTreeNode);
            var newTreeNode = _treeNodePools[treeNodeType].TakeNode<TTreeNode>();

            if(TreeNodeGrid.TryGetValue((x, y), out var treeNode) && treeNode is PathTreeNode)
            {
                TreeNodeGrid.Add((x, y), newTreeNode);
            }
            else 
            {
                if(!_treeNodePools.ContainsKey(treeNodeType))
                    _treeNodePools[treeNodeType] = new Pool<ITreeNode>();

                TreeNodeGrid.Add((x, y), newTreeNode);
            }
        }

        public void RemoveNode<TTreeNode>(int x, int y)
            where TTreeNode : ITreeNode 
        {
            if(TreeNodeGrid.TryGetValue((x, y), out var treeNode))
            {
                var treeNodeType = typeof(TTreeNode);
                _treeNodePools[treeNodeType].ReturnNode<ITreeNode>(treeNode);
            }

        }

        public void ResetTrees()
        {
            foreach (var tree in Trees)
                tree.ResetTree();
        }
    }
}