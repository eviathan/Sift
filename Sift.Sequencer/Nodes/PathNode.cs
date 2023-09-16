using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    public class PathNode : Node
    {
        // NOTE: I dont think we need direction
        // public Direction Direction { get; set; }
        
        public PathNode(
            Position transform,
            Node? parent = null,
            List<Node>? children = null
        ) : base(transform, parent, children) { }
    }
}