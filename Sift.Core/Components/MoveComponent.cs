using System.ComponentModel.DataAnnotations;
using Sift.Core.Commands;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record MoveConfig(Direction Direction = Direction.Right, [Range(1, 8)] int Distance = 1) : IComponentConfig
    {
        public void Validate()
        {
            if (Distance is < 1 or > 8)
                throw new ArgumentOutOfRangeException(nameof(Distance), Distance, "Distance must be 1-8.");
        }
    }

    // NOTE: Moves its own node each time it's triggered, so the routes rewrite themselves as the sequence plays. Blocked
    // when the cell is taken. Moves made while playing are undone when playback stops
    [ComponentType("move", Category = ComponentCategory.Motion)]
    public class MoveComponent : Component<MoveConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var to = context.Node.Position + Config.Direction.ToVector() * Config.Distance;

            // NOTE: A blocked move is rejected by the runtime, which is the behaviour we want
            _ = context.Sequencer.Submit(new MoveNode(context.Node.Id, to));

            return Task.CompletedTask;
        }
    }
}
