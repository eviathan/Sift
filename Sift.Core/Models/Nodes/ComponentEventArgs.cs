using Sift.Core.Nodes;

namespace Sift.Core.Models.Nodes
{
    public class ComponentEventArgs
    {
        public Sequence Sequence;
        public Node Node = null!;

        public ComponentEventArgs(Sequence sequence)
        {
            Sequence = sequence;
        }
    }
}