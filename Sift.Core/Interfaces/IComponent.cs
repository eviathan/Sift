using Sift.Core.Models.Nodes;

namespace Sift.Core.Interfaces
{
    public interface IComponent
    {
        Task InvokeAsync(ComponentEventArgs args, CancellationToken cancellationToken);
    }
}