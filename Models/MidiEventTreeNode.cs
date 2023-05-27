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

        void ITreeNode.DidStart()
        {
            Console.WriteLine($"Note On: {Event.Pitch}");
            MIDIService.Instance.SendMidiNoteOn(Event.Pitch);
        }

        public void DidEnd()
        {
            Console.WriteLine($"Note Off: {Event.Pitch}");
            MIDIService.Instance.SendMidiNoteOff(Event.Pitch);
        }
    }
}