using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public class LogComponent : IComponent, IListener
    {
        public async Task InvokeAsync(ComponentEventArgs args, CancellationToken cancellationToken)
        {
            Console.WriteLine($"LogNodeEvent Invocation: {args.Node.Position.X}_{args.Node.Position.Y}_{args.Node.Position.Z}");
        }

        public Task OnReceivedAsync(ComponentEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}