using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Models.Nodes
{
    public class NodeEventArgs
    {
        public Sequence Sequence { get; set; }
        public Node Node { get; set; }
    }
}