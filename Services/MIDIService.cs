using Commons.Music.Midi;

public static class MIDIService
{
    public static async Task SendMidiNote(int note)
    {
        // Get all MIDI output devices
        var midiAccess = MidiAccessManager.Default;
        var outputDevice = midiAccess.Outputs.ElementAt(1);

        // Find first output device
        if (outputDevice == null)
        {
            Console.WriteLine("No MIDI output devices found.");
            return;
        }
        Console.WriteLine($"Node: {note}");

        // Open the device and send MIDI message
        var midiOutput = await midiAccess.OpenOutputAsync(outputDevice.Id);
        midiOutput.Send(new byte[] { 0x90, (byte)note, 0x7F }, 0, 3, 0); // Note on
        await Task.Delay(200);
        midiOutput.Send(new byte[] { 0x80, (byte)note, 0x7F }, 0, 3, 0); // Note off
    }
}