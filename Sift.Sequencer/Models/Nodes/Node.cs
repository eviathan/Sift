using Sift.Sequencer.Grid;
using Sift.Sequencer.Interfaces;

namespace Sift.Sequencer.Nodes
{
    public abstract class Node
    {
        public bool IsActive { get; set; }
        public GridVector Position { get; private set; }
        public List<INodeEvent> Events { get; set; } = new();
        public List<GridVector> Children { get; set; } = new();

        public Node(GridVector position, List<GridVector>? children = null)
        {
            Position = position;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }
    }
}