using Sift.Sequencer.Grid;
using Sift.Sequencer.Interfaces;

namespace Sift.Sequencer.Nodes
{
    public abstract class Node
    {
        public bool IsRoot { get; set; }
        public bool IsActive { get; set; }
        public Vector3D Position { get; private set; }
        public List<INodeEvent> Events { get; set; } = new();

        public Node(Vector3D position, bool isActive = true, bool isRoot = false)
        {
            Position = position;
            IsActive = isActive;
            IsRoot = isRoot;
        }

        public virtual void DidStart() { }

        public virtual void DidEnd() { }
    }
}