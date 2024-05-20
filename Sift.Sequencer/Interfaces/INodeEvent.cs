using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Interfaces
{
    public interface INodeEvent
    {
        void Invoke(NodeEventArgs e);
    }
}