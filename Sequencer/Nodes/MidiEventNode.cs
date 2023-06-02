using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer.Nodes
{
    public class MidiEventNode : INode
    {
        public INode? Parent { get; set; }
        public List<INode> Children { get; set; }
        public MIDIEvent MIDIEvent { get; set; }

        public MidiEventNode(MIDIEvent midiEvent, List<INode>? children = null)
        {
            MIDIEvent = midiEvent;
            Children = children ?? new List<INode>();

            foreach (var child in Children)
                child.Parent = this;
        }   

        void INode.DidStart()
        {
            MIDIService.Instance.SendMidiNoteOn(MIDIEvent.Pitch);
        }

        public void DidEnd()
        {
            MIDIService.Instance.SendMidiNoteOff(MIDIEvent.Pitch);
        }
    }
}