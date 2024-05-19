using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Interfaces
{
    public interface INodeEvent
    {
        void Invoke(Node node);
    }
}