using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    // NOTE: We might be able to move all of the GridNode code into this
    // abstract class as its probably a bit redundant backing it with an interface
    public abstract class Node
    {
        public bool IsRoot { get; set; }
        public Node? Parent { get; set; }
        public List<Node> Children { get; private set; }
        public Position Position { get; private set; }

        public double DistanceFromRoot { get; set; }
        public double DistanceFromParent { get; set; }

        public Node(
            Position position,
            Node? parent = null,
            List<Node>? children = null
        )
        {
            Position = position;
            Parent = parent;

            DistanceFromParent = Parent != null 
                ? Position.Distance(Parent.Position)
                : 0;

            Children = children ?? new List<Node>();
            foreach (var child in Children)
                child.Parent = this;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }
    }
}