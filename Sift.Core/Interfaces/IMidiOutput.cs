namespace Sift.Core.Interfaces
{
    // NOTE: Safe to call from any thread. Channels are 1-16, notes and velocities 0-127
    public interface IMidiOutput
    {
        bool IsAvailable { get; }

        void NoteOn(int channel, int note, int velocity);

        void NoteOff(int channel, int note);

        void AllNotesOff();
    }
}
