using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record GateConfig(bool StartOpen = true, Direction? To = null) : IComponentConfig;

    // NOTE: Lets messages through while the node's gate is open. A Toggle on another of the node's ports opens and closes it
    [ComponentType("gate", Category = ComponentCategory.Flow)]
    public class GateComponent : Component<GateConfig>
    {
        public const string STATE = "gate.open";

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            if (context.State.Get(STATE, Config.StartOpen))
                Forward(context, Config.To);

            return Task.CompletedTask;
        }
    }

    public sealed record ToggleConfig(bool StartOpen = true) : IComponentConfig;

    // NOTE: Flips the node's gate each time a message arrives. StartOpen should match the node's Gate
    [ComponentType("toggle", Category = ComponentCategory.Flow)]
    public class ToggleComponent : Component<ToggleConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            context.State.Update(GateComponent.STATE, Config.StartOpen, open => !open);

            return Task.CompletedTask;
        }
    }
}
