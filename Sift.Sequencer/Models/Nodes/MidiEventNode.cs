// using Sift.Sequencer.Grid;
// using Sift.Sequencer.MIDI;

// namespace Sift.Sequencer.Nodes
// {
//     public class MidiEventNode : Node
//     {
//         public MIDIEvent MIDIEvent { get; set; }

//         public MidiEventNode(MIDIEvent midiEvent, Position position, List<GridVector>? children = null)
//             : base(position, children)
//         {
//             MIDIEvent = midiEvent;
//         }

//         public override void DidStart()
//         {
//             MIDIService.Instance.SendMidiNoteOn(MIDIEvent.Pitch);
//         }

//         public override void DidEnd()
//         {
//             MIDIService.Instance.SendMidiNoteOff(MIDIEvent.Pitch);
//         }
//     }
// }