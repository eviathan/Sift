using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    // NOTE: Repeats is how many messages to send, 0 keeps going until playback stops
    public sealed record ClockConfig(NoteDuration Every = NoteDuration.QuarterNote, [Range(0, 64)] int Repeats = 0) : IComponentConfig
    {
        public void Validate()
        {
            if (!Enum.IsDefined(Every))
                throw new ArgumentOutOfRangeException(nameof(Every), Every, "Unknown note duration.");

            if (Repeats is < 0 or > 64)
                throw new ArgumentOutOfRangeException(nameof(Repeats), Repeats, "Repeats must be 0-64.");
        }
    }

    // NOTE: Sends a message out of its port straight away and then every Every. Triggering it again restarts it
    [ComponentType("clock", Category = ComponentCategory.Source)]
    public class ClockComponent : Component<ClockConfig>
    {
        private readonly object _sync = new();
        private CancellationTokenSource? _loop;

        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            CancellationTokenSource loop;

            lock (_sync)
            {
                _loop?.Cancel();
                _loop = loop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            var time = context.DispatchTime;
            var sent = 0;

            try
            {
                while (!loop.IsCancellationRequested)
                {
                    context.Sequencer.Emit(context.Node.Id, context.Port, new Message { Payload = "clock", Transpose = context.Message.Transpose }, time);

                    if (Config.Repeats > 0 && ++sent >= Config.Repeats)
                        break;

                    // NOTE: Read each time round so changing the rate takes effect on the next beat
                    time = await context.Sequencer.WaitUntilAsync(time + Config.Every.ToTicks(), loop.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
