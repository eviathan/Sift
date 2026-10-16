using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;
using Sift.Core.MIDI;

namespace Sift.Core.Components
{
    public sealed record MidiConfig(int Note = 60, int Velocity = 100, int Channel = 1, NoteDuration Duration = NoteDuration.SixteenthNote) : IComponentConfig;

    // TODO: Phase 5, send the configured velocity/channel once MIDIService supports them
    [ComponentType("midi")]
    public class MIDIComponent : Component<MidiConfig>
    {
        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            MIDIService.Instance.SendMidiNoteOn(Config.Note);

            try
            {
                await context.Sequencer.WaitUntilAsync(context.DispatchTime + Config.Duration.ToTicks(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                MIDIService.Instance.SendMidiNoteOff(Config.Note);
            }
        }
    }
}
