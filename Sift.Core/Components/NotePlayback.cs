using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Extensions;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    // NOTE: Plays notes for a duration in musical time with the message's transpose applied. The note offs are always
    // sent, even when cancelled
    internal static class NotePlayback
    {
        public static async Task PlayAsync(IMidiOutput output, ComponentContext context, IEnumerable<int> notes, int velocity, int channel, NoteDuration duration, CancellationToken cancellationToken)
        {
            var played = notes.Select(x => Math.Clamp(x + context.Message.Transpose, 0, 127)).Distinct().ToList();

            foreach (var note in played)
            {
                output.NoteOn(channel, note, velocity);
                context.Sequencer.Publish(new NoteOn(context.Node.Id, channel, note, velocity));
            }

            try
            {
                await context.Sequencer.WaitUntilAsync(context.DispatchTime + duration.ToTicks(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                foreach (var note in played)
                {
                    output.NoteOff(channel, note);
                    context.Sequencer.Publish(new NoteOff(context.Node.Id, channel, note));
                }
            }
        }
    }
}
