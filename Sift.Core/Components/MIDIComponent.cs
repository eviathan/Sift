using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;
using Sift.Core.MIDI;

namespace Sift.Core.Components
{
    public sealed record MidiConfig(int Note = 60, int Velocity = 100, int Channel = 1, NoteDuration Duration = NoteDuration.SixteenthNote) : IComponentConfig;

    // TODO: Phase 5, hold the note for Duration using musical time and send the configured velocity/channel
    [ComponentType("midi")]
    public class MIDIComponent : Component<MidiConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            MIDIService.Instance.SendMidiNoteOn(Config.Note);
            MIDIService.Instance.SendMidiNoteOff(Config.Note);

            return Task.CompletedTask;
        }
    }
}
