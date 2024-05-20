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
                var vector = direction.Key * (direction.Value ?? 1);
                e.Sequence.DispatchWalker(e.Node, vector);
            }
        }
    }
}