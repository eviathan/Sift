using Sift.Sequencer.Grid;
using Sift.Sequencer.NodeEvents;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Models.Nodes
{
    public class TestNode : Node
    {
        public TestNode(Vector3D position, bool emitOnPlay = false) : base(position, emitOnPlay: emitOnPlay)
        {
            Events = [new LogNodeEvent()];
        }
    }
}
