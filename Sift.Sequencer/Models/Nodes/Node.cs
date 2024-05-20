using Sift.Sequencer.Grid;
using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Models.Nodes;

namespace Sift.Sequencer.Nodes
{
    public abstract class Node
    {
        public bool IsActive { get; set; }
        public bool EmitOnPlay { get; set; }
        public Vector3D Position { get; private set; }
        public List<INodeEvent> Events { get; set; } = [];

        public Node(Vector3D position, bool isActive = true, bool emitOnPlay = false)
        {
            Position = position;
            IsActive = isActive;
            EmitOnPlay = emitOnPlay;
        }

        public virtual void DidStart(NodeEventArgs e) { }

        public virtual void DidEnd(NodeEventArgs e) { }
    }
}