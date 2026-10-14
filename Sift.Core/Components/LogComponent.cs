using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public class LogComponent : IComponent, IListener
    {
        public async Task OnReceivedAsync(NodeEventArgs e)
        {
            Console.WriteLine($"LogNodeEvent Invocation: {e.Node.Position.X}_{e.Node.Position.Y}_{e.Node.Position.Z}");
        }
    }
}