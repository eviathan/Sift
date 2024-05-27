using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Models.Nodes;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.NodeEvents
{
    public class MIDINodeEvent : INodeEvent
    {
        public void Invoke(NodeEventArgs e)
        {
            MIDIService.Instance.SendMidiNoteOn(0);
            MIDIService.Instance.SendMidiNoteOff(0);
        }
    }
}