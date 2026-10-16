using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Nodes
{
    public abstract class Node : Entity
    {
        // TODO: Move this into a contenxt if this grows
        // (I dont want to have to inject all of that seperately every time I extend Node)
        public readonly NodeBus Bus;

        public bool IsActive { get; set; }
        public bool EmitOnPlay { get; set; }
        public Vector3D Position { get; private set; }

        public Node(NodeBus bus)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));

            // Vector3D position, bool isActive = true, bool emitOnPlay = false
            // Position = position;
            // IsActive = isActive;
            // EmitOnPlay = emitOnPlay;            
        }

        public Port GetPort(Direction direction)
        {
            return Bus[direction];
        }

        public virtual void DidStart(ComponentEventArgs e) { }

        public virtual void DidEnd(ComponentEventArgs e) { }
    }
}