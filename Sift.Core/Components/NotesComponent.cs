using System.ComponentModel.DataAnnotations;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;
using Sift.Core.Music;

namespace Sift.Core.Components
{
    public enum NoteOrder
    {
        Up,
        Down,
        UpDown,
        Random
    }

    public sealed record NotesConfig(
        string Notes = "C4 E4 G4 B4",
        NoteOrder Order = NoteOrder.Up,
        [Range(1, 127)] int Velocity = 100,
        [Range(1, 16)] int Channel = 1,
        NoteDuration Duration = NoteDuration.SixteenthNote) : IComponentConfig
    {
        public void Validate()
        {
            try
            {
                if (NoteNames.ParseList(Notes).Count == 0)
                    throw new ArgumentException("Notes needs at least one note.", nameof(Notes));
            }
            catch (FormatException ex)
            {
                throw new ArgumentException(ex.Message, nameof(Notes), ex);
            }

            if (Velocity is < 1 or > 127)
                throw new ArgumentOutOfRangeException(nameof(Velocity), Velocity, "Velocity must be 1-127.");

            if (Channel is < 1 or > 16)
                throw new ArgumentOutOfRangeException(nameof(Channel), Channel, "Channel must be 1-16.");

            if (!Enum.IsDefined(Duration))
                throw new ArgumentOutOfRangeException(nameof(Duration), Duration, "Unknown note duration.");
        }
    }

    // NOTE: Each message plays the next note from the list, so one node can carry a melody or an arpeggio
    [ComponentType("notes", Category = ComponentCategory.Sound)]
    public class NotesComponent : Component<NotesConfig>
    {
        private readonly IMidiOutput _midiOutput;

        public NotesComponent(IMidiOutput midiOutput)
        {
            _midiOutput = midiOutput ?? throw new ArgumentNullException(nameof(midiOutput));
        }

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var config = Config;
            var notes = NoteNames.ParseList(config.Notes);
            var step = context.State.Update($"notes.{Id}", -1, x => x + 1);

            return NotePlayback.PlayAsync(_midiOutput, context, [Pick(notes, step, config.Order)], config.Velocity, config.Channel, config.Duration, cancellationToken);
        }

        public static int Pick(IReadOnlyList<int> notes, int step, NoteOrder order)
        {
            var count = notes.Count;

            return order switch
            {
                NoteOrder.Down => notes[count - 1 - step % count],
                NoteOrder.UpDown when count > 1 => notes[PingPong(step, count)],
                NoteOrder.Random => notes[Random.Shared.Next(count)],
                _ => notes[step % count]
            };
        }

        // NOTE: 0 1 2 3 2 1 0 1 … without repeating the ends
        private static int PingPong(int step, int count)
        {
            var period = (count - 1) * 2;
            var position = step % period;
            return position < count ? position : period - position;
        }
    }
}
