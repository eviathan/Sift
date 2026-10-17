using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record TransposeConfig([Range(-48, 48)] int Semitones = 12, Direction? To = null) : IComponentConfig
    {
        public void Validate()
        {
            if (Semitones is < -48 or > 48)
                throw new ArgumentOutOfRangeException(nameof(Semitones), Semitones, "Semitones must be -48 to 48.");
        }
    }

    // NOTE: Shifts every note the message plays from here on, then sends it on
    [ComponentType("transpose", Category = ComponentCategory.Sound)]
    public class TransposeComponent : Component<TransposeConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            Forward(context, Config.To, context.Message.WithTranspose(Config.Semitones));

            return Task.CompletedTask;
        }
    }
}
