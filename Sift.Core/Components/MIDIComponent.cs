using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record MidiConfig(int Note = 60, int Velocity = 100, int Channel = 1, NoteDuration Duration = NoteDuration.SixteenthNote) : IComponentConfig
    {
        public void Validate()
        {
            if (Note is < 0 or > 127)
                throw new ArgumentOutOfRangeException(nameof(Note), Note, "Note must be 0-127.");

            if (Velocity is < 1 or > 127)
                throw new ArgumentOutOfRangeException(nameof(Velocity), Velocity, "Velocity must be 1-127.");

            if (Channel is < 1 or > 16)
                throw new ArgumentOutOfRangeException(nameof(Channel), Channel, "Channel must be 1-16.");

            if (!Enum.IsDefined(Duration))
                throw new ArgumentOutOfRangeException(nameof(Duration), Duration, "Unknown note duration.");
        }
    }

    // NOTE: Plays a note for Duration in musical time. The note off is always sent, even when cancelled
    [ComponentType("midi")]
    public class MIDIComponent : Component<MidiConfig>
    {
        private readonly IMidiOutput _midiOutput;

        public MIDIComponent(IMidiOutput midiOutput)
        {
            _midiOutput = midiOutput ?? throw new ArgumentNullException(nameof(midiOutput));
        }

        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            // NOTE: Captured so a config change mid-note still turns off the note that was played
            var config = Config;

            _midiOutput.NoteOn(config.Channel, config.Note, config.Velocity);

            try
            {
                await context.Sequencer.WaitUntilAsync(context.DispatchTime + config.Duration.ToTicks(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _midiOutput.NoteOff(config.Channel, config.Note);
            }
        }
    }
}
