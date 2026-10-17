using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record DelayConfig(NoteDuration Length = NoteDuration.EighthNote, Direction? To = null) : IComponentConfig
    {
        public void Validate()
        {
            if (!Enum.IsDefined(Length))
                throw new ArgumentOutOfRangeException(nameof(Length), Length, "Unknown note duration.");
        }
    }

    // NOTE: Holds a message for Length then sends it on
    [ComponentType("delay", Category = ComponentCategory.Time)]
    public class DelayComponent : Component<DelayConfig>
    {
        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            try
            {
                var at = await context.Sequencer.WaitUntilAsync(context.DispatchTime + Config.Length.ToTicks(), cancellationToken);
                Forward(context, Config.To, at: at);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
