using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record SplitConfig(bool Up = true, bool Right = true, bool Down = true, bool Left = true, bool Forwards = false, bool Backwards = false) : IComponentConfig
    {
        public IEnumerable<Direction> Directions()
        {
            if (Up) yield return Direction.Up;
            if (Right) yield return Direction.Right;
            if (Down) yield return Direction.Down;
            if (Left) yield return Direction.Left;
            if (Forwards) yield return Direction.Forwards;
            if (Backwards) yield return Direction.Backwards;
        }
    }

    // NOTE: Copies the message out of every chosen direction except back where it came from
    [ComponentType("split", Category = ComponentCategory.Flow)]
    public class SplitComponent : Component<SplitConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            foreach (var direction in Config.Directions())
            {
                if (context.IsPlay || direction != context.Port)
                    Forward(context, direction);
            }

            return Task.CompletedTask;
        }
    }
}
