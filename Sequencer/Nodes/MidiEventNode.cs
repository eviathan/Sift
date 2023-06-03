using Sift.Sequencer.Grid;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.Nodes
{
    public class MidiEventNode : GridNode
    {
        public MIDIEvent MIDIEvent { get; set; }

        public MidiEventNode(
            MIDIEvent midiEvent,
            Position position,
            INode? parent = null,
            List<INode>? children = null)
                : base(position, parent, children)
        {
            MIDIEvent = midiEvent;
        }   

        public override void DidStart()
        {
            MIDIService.Instance.SendMidiNoteOn(MIDIEvent.Pitch);
        }

        public override void DidEnd()
        {
            MIDIService.Instance.SendMidiNoteOff(MIDIEvent.Pitch);
        }
    }
}