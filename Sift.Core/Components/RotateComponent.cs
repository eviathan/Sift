using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record RotateConfig(bool Clockwise = true) : IComponentConfig;

    // NOTE: Sends each message out of the next side in turn (up, right, down, left), so a stream of messages sweeps round
    [ComponentType("rotate", Category = ComponentCategory.Flow)]
    public class RotateComponent : Component<RotateConfig>
    {
        private static readonly Direction[] CLOCKWISE = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var step = context.State.Update($"rotate.{Id}", -1, x => x + 1);
            var index = step % CLOCKWISE.Length;

            Forward(context, CLOCKWISE[Config.Clockwise ? index : (CLOCKWISE.Length - index) % CLOCKWISE.Length]);

            return Task.CompletedTask;
        }
    }
}
