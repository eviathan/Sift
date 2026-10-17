using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;
using Sift.Core.Music;

namespace Sift.Core.Components
{
    public sealed record ChordConfig(
        [Range(0, 127)] int Root = 60,
        ChordQuality Quality = ChordQuality.Major,
        [Range(0, 3)] int Inversion = 0,
        [Range(1, 127)] int Velocity = 100,
        [Range(1, 16)] int Channel = 1,
        NoteDuration Duration = NoteDuration.QuarterNote) : IComponentConfig
    {
        public void Validate()
        {
            if (Root is < 0 or > 127)
                throw new ArgumentOutOfRangeException(nameof(Root), Root, "Root must be 0-127.");

            if (Inversion is < 0 or > 3)
                throw new ArgumentOutOfRangeException(nameof(Inversion), Inversion, "Inversion must be 0-3.");

            if (Velocity is < 1 or > 127)
                throw new ArgumentOutOfRangeException(nameof(Velocity), Velocity, "Velocity must be 1-127.");

            if (Channel is < 1 or > 16)
                throw new ArgumentOutOfRangeException(nameof(Channel), Channel, "Channel must be 1-16.");

            if (!Enum.IsDefined(Duration))
                throw new ArgumentOutOfRangeException(nameof(Duration), Duration, "Unknown note duration.");
        }

        // NOTE: Each inversion moves the lowest note up an octave
        public IReadOnlyList<int> Notes()
        {
            var notes = Chords.Intervals(Quality).Select(x => Root + x).ToList();

            for (var i = 0; i < Inversion % notes.Count; i++)
                notes[i] += 12;

            return notes;
        }
    }

    [ComponentType("chord", Category = ComponentCategory.Sound)]
    public class ChordComponent : Component<ChordConfig>
    {
        private readonly IMidiOutput _midiOutput;

        public ChordComponent(IMidiOutput midiOutput)
        {
            _midiOutput = midiOutput ?? throw new ArgumentNullException(nameof(midiOutput));
        }

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var config = Config;

            return NotePlayback.PlayAsync(_midiOutput, context, config.Notes(), config.Velocity, config.Channel, config.Duration, cancellationToken);
        }
    }
}
