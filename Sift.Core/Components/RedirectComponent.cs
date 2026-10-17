using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record RedirectConfig(Direction To = Direction.Up) : IComponentConfig;

    [ComponentType("redirect")]
    public class RedirectComponent : Component<RedirectConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            context.Sequencer.Emit(context.Node.Id, Config.To, context.Message, context.DispatchTime);

            return Task.CompletedTask;
        }
    }
}
