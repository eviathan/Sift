using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.Components
{
    public class MIDIComponent : IComponent
    {
        public async Task InvokeAsync(NodeEventArgs e)
        {
            MIDIService.Instance.SendMidiNoteOn(0);
            MIDIService.Instance.SendMidiNoteOff(0);
        }
    }
}