using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace Sift.Sequencer.MIDI
{
    public class MIDIService
    {
        public static MIDIService Instance => instance;
        private static readonly MIDIService instance = new MIDIService();

        private const string MIDI_DEVICE_NAME = "Sift MIDI";

        private VirtualDevice _virtualDevice;
        private OutputDevice _outputDevice;

        static MIDIService() { }

        private MIDIService()
        {
            _virtualDevice = VirtualDevice.Create(MIDI_DEVICE_NAME);
            _outputDevice = OutputDevice.GetByName(MIDI_DEVICE_NAME);
        }

        public void SendMidiNoteOn(int note)
        {
            if (_outputDevice == null)
            {
                Console.WriteLine($"MIDI output device '{MIDI_DEVICE_NAME}' not found.");
                return;
            }

            Console.WriteLine($"Note On: {note}");

            try
            {
                // using (_outputDevice)
                // {
                // Send Note On (Channel 1, Note number 60 (Middle C), Velocity 100)
                var noteOnEvent = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100);
                _outputDevice.SendEvent(noteOnEvent);

                // Wait for 1 second
                // Thread.Sleep(1000);

                // Send Note Off (Channel 1, Note number 60 (Middle C), Velocity 0)
                var noteOffEvent = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)100);
                _outputDevice.SendEvent(noteOffEvent);

                Console.WriteLine("MIDI Note On and Note Off events sent successfully.");
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public void SendMidiNoteOff(int note)
        {
            Console.WriteLine($"Note Off: {note}");
        }

        public void KillAllNotes()
        {
            // for (int i = 0; i < 127; i++)
            // {
            //     var noteOffEvent = new NoteOffEvent((SevenBitNumber)i, (SevenBitNumber)0);
            //     _outputDevice.SendEvent(noteOffEvent);
            // }
        }
    }
}