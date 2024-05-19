using System.Numerics;
using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    public abstract class Node
    {
        public List<GridVector> Children { get; set; } = new();
        public Position Position { get; private set; }

        public Node(Position position, List<GridVector>? children = null)
        {
            Position = position;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }
    }
}