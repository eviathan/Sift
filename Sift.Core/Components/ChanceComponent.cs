using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record ChanceConfig([Range(0, 100)] int Percent = 50, Direction? To = null) : IComponentConfig
    {
        public void Validate()
        {
            if (Percent is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(Percent), Percent, "Percent must be 0-100.");
        }
    }

    // NOTE: Lets a message through Percent of the time
    [ComponentType("chance", Category = ComponentCategory.Flow)]
    public class ChanceComponent : Component<ChanceConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            if (Random.Shared.Next(100) < Config.Percent)
                Forward(context, Config.To);

            return Task.CompletedTask;
        }
    }
}
