using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;

namespace Sift.App.Scene
{
    // NOTE: Which way a component on a port sends messages, for drawing the routes. Approximate for the random and
    // rotating ones (they could go any of their directions)
    public static class Routes
    {
        private static readonly Direction[] PLANE = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

        public static IEnumerable<Direction> Directions(Direction port, IComponentConfig config) => config switch
        {
            EmitConfig or ClockConfig => [port],
            RedirectConfig redirect => [redirect.To],
            SplitConfig split => split.Directions().Where(x => x != port),
            RotateConfig => PLANE,
            ScatterConfig scatter => scatter.Layers ? [.. PLANE, Direction.Forwards, Direction.Backwards] : PLANE,
            _ when config.GetType().GetProperty("To")?.GetValue(config) is Direction to => [to],
            _ when config.GetType().GetProperty("To")?.PropertyType == typeof(Direction?) => [port.Opposite()],
            _ => []
        };
    }
}
