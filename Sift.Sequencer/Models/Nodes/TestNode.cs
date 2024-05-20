using Sift.Sequencer.Grid;
using Sift.Sequencer.NodeEvents;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Models.Nodes
{
    public class TestNode : Node
    {
        public TestNode(Vector3D position, bool isRoot = false) : base(position, isRoot: isRoot)
        {
            Events = [new LogNodeEvent()];
        }
    }
}
