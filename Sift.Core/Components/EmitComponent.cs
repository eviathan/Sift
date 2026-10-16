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

        public async Task InvokeAsync(ComponentEventArgs args, CancellationToken cancellationToken)
        {
            foreach (var direction in _directions)
            {
                Console.WriteLine($"Emit: Node:({args.Node.Position})_Direction:({direction})");

                var vector = direction.Key * (direction.Value ?? 1);
            }
        }
    }
}