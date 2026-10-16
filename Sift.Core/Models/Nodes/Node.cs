using Sift.Core.Enums;
using Sift.Core.Grid;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Nodes
{
    // NOTE: Nodes are built by composition, behaviour lives in the components on each port
    public class Node : Entity
    {
        public NodeBus Bus { get; } = new();

        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmitOnPlay { get; set; }
        public Vector3D Position { get; internal set; }

        public Node(Vector3D position, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            Position = position;
        }

        public Port GetPort(Direction direction) =>
            Bus[direction];
    }
}
