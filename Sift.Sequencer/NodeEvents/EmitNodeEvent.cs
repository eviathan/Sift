using Sift.Sequencer.Grid;
using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Models.Nodes;

namespace Sift.Sequencer.NodeEvents
{
    public class EmitNodeEvent : INodeEvent
    {
        private Dictionary<Vector3D, int?> _directions { get; set; }

        public EmitNodeEvent(Dictionary<Vector3D, int?> directions)
        {
            _directions = directions;
        }

        public void Invoke(NodeEventArgs e)
        {
            foreach (var direction in _directions)
            {
                Console.WriteLine($"Emit: Node:({e.Node.Position})_Direction:({direction})");

                var vector = direction.Key * (direction.Value ?? 1);
                e.Sequence.EnqueueWalker(e.Node, vector);
            }
        }
    }
}