using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Interfaces
{
    public interface INodeEvent
    {
        // I should pass in the Grid, Walker, Sequence etc in the Event
        void Invoke(NodeEventArgs e);
    }
}