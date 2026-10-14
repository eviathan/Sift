using Sift.Core.Models.Nodes;

namespace Sift.Core.Interfaces
{
    public interface IEmitter
    {
        Task EmitAsync(NodeEventArgs e);
    }
}