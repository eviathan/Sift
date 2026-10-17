using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record DivideConfig([Range(1, 16)] int Every = 2, Direction? To = null) : IComponentConfig
    {
        public void Validate()
        {
            if (Every is < 1 or > 16)
                throw new ArgumentOutOfRangeException(nameof(Every), Every, "Every must be 1-16.");
        }
    }

    // NOTE: Lets through one message in every Every, the first of each group. The count restarts with playback
    [ComponentType("divide", Category = ComponentCategory.Flow)]
    public class DivideComponent : Component<DivideConfig>
    {
        private const string COUNT = "divide.count";

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var count = context.State.Update($"{COUNT}.{Id}", 0, x => x + 1);

            if ((count - 1) % Config.Every == 0)
                Forward(context, Config.To);

            return Task.CompletedTask;
        }
    }
}
