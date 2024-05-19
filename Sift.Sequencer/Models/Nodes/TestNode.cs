using Sift.Sequencer.Grid;
using Sift.Sequencer.NodeEvents;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Models.Nodes
{
    public class TestNode : Node
    {
        public TestNode(GridVector position, List<GridVector>? children = null) : base(position, children)
        {
            Events = [new LogNodeEvent()];
        }
    }
}
