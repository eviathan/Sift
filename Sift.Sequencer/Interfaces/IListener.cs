using Sift.Sequencer.Models.Nodes;

namespace Sift.Sequencer.Interfaces
{
    public interface IListener
    {
        Task OnReceivedAsync(NodeEventArgs e);
    }
}