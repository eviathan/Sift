using Sift.Core.Models.Nodes;

namespace Sift.Core.Interfaces
{
    public interface IListener
    {
        Task OnReceivedAsync(NodeEventArgs e);
    }
}