using Sift.Sequencer.Models.Nodes;

namespace Sift.Sequencer.Interfaces
{
    public interface IEmitter
    {
        Task EmitAsync(NodeEventArgs e);
    }
}