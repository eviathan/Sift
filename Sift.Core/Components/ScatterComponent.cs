using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    // NOTE: Layers lets it pick forwards/backwards (to the layers above and below) as well
    public sealed record ScatterConfig(bool Layers = false) : IComponentConfig;

    // NOTE: Sends the message on in a random direction, never straight back where it came from
    [ComponentType("scatter", Category = ComponentCategory.Flow)]
    public class ScatterComponent : Component<ScatterConfig>
    {
        private static readonly Direction[] PLANE = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];
        private static readonly Direction[] ALL = [.. PLANE, Direction.Forwards, Direction.Backwards];

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var choices = (Config.Layers ? ALL : PLANE).Where(x => context.IsPlay || x != context.Port).ToArray();

            Forward(context, choices[Random.Shared.Next(choices.Length)]);

            return Task.CompletedTask;
        }
    }
}
