using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class MIDIEvent
    {
        public int TimeStamp { get; set; }

        // NOTE: 7bit MIDI note values (Default: Middle C)
        public short Pitch { get; set; } = 60;

        public short Velocity { get; set; }

        // NOTE Normalised 0.0f - 1.0f which is a percentage of the length between this node and the next
        // When end node we may need to have this function specify the actual note value but for now lets just have it terminate on a 
        // Semi quaver
        public float Duration { get; set; } = 1.0f;
    }
}