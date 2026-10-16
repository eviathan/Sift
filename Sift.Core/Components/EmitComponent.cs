using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record EmitConfig : IComponentConfig;

    // NOTE: Emits out of the port it sits on
    [ComponentType("emit")]
    public class EmitComponent : Component<EmitConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            context.Sequencer.Emit(context.Node.Id, context.Port, context.Message, context.DispatchTime);

            return Task.CompletedTask;
        }
    }
}
