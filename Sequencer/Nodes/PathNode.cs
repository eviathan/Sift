using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    public class PathNode : GridNode
    {
        public Direction Direction { get; set; }
        
        public PathNode(Position transform, INode? parent = null, List<INode>? children = null) : base(transform, parent, children)
        {
        }
    }
}