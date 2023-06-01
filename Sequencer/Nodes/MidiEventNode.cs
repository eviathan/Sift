using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.Nodes
{
    public class MidiEventNode : INode
    {
        public INode Next { get; set; }

        public MIDIEvent Event { get; set; }

        void INode.DidStart()
        {
            MIDIService.Instance.SendMidiNoteOn(Event.Pitch);
        }

        public void DidEnd()
        {
            MIDIService.Instance.SendMidiNoteOff(Event.Pitch);
        }
    }
}