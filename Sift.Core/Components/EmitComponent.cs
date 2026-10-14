using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public class EmitComponent : IComponent
    {
        private Dictionary<Vector3D, int?> _directions { get; set; }

        public EmitComponent(Dictionary<Vector3D, int?> directions)
        {
            _directions = directions;
        }

        public async Task InvokeAsync(NodeEventArgs e)
        {
            foreach (var direction in _directions)
            {
                Console.WriteLine($"Emit: Node:({e.Node.Position})_Direction:({direction})");

                var vector = direction.Key * (direction.Value ?? 1);
                e.Sequence.EnqueueWalker(e.Node, vector);
            }

            return;
        }
    }
}