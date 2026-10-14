using Sift.Core.Nodes;

namespace Sift.Core.Models.Nodes
{
    public class NodeEventArgs
    {
        public Sequence Sequence { get; set; } = null!;
        public Node Node { get; set; } = null!;
    }
}