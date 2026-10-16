using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;
using Sift.Core.MIDI;

namespace Sift.Core.Components
{
    public class MIDIComponent : IComponent
    {
        public async Task InvokeAsync(ComponentEventArgs args, CancellationToken cancellationToken)
        {
            MIDIService.Instance.SendMidiNoteOn(0);
            MIDIService.Instance.SendMidiNoteOff(0);
        }
    }
}