using Sift.Sequencer.Enums;
using Sift.Sequencer.Grid;

namespace Sift.Sequencer.Nodes
{
    public abstract class GridNode : INode
    {
        public INode? Parent { get; set; }
        public List<INode> Children { get; private set; }
        public Position Position { get; private set; }
        public int DistanceFromParent { get; private set; }

        public GridNode(
            Position position,
            INode? parent = null,
            List<INode>? children = null)
        {
            Position = position;
            Parent = parent;
            DistanceFromParent = 0; //GetDistanceFromParent();

            Children = children ?? new List<INode>();
            foreach (var child in Children)
                child.Parent = this;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }

        private int GetDistanceFromParent()
        {
            throw new NotImplementedException();
            // return Parent.Position.X
        }
    }
}