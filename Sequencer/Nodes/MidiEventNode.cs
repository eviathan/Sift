using Sift.Sequencer.Grid;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.Nodes
{
    public class MidiEventNode : Node
    {
        public MIDIEvent MIDIEvent { get; set; }

        public MidiEventNode(
            MIDIEvent midiEvent,
            Position position,
            Node? parent = null,
            List<Node>? children = null
        ) : base(position, parent, children)
        {
            MIDIEvent = midiEvent;
        }   

        public override void DidStart()
        {
            Console.WriteLine($"Note On: {this}");
            // MIDIService.Instance.SendMidiNoteOn(MIDIEvent.Pitch);
        }

        public override void DidEnd()
        {
            Console.WriteLine($"Note Off: {this}");
            // MIDIService.Instance.SendMidiNoteOff(MIDIEvent.Pitch);
        }

        public override string ToString()
        {
            return $"{MIDIEvent.Pitch} {MIDIEvent.Duration} {MIDIEvent.Velocity}";
        }
    }
}