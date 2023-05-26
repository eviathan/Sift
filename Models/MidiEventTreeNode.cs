using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class MidiEventTreeNode : ITreeNode
    {
        public ITreeNode Next { get; set; }

        public MIDIEvent Event { get; set; }

        void ITreeNode.Invoke()
        {
            MIDIService.Instance.SendMidiNoteOn(Event.Pitch);
        }
    }
}