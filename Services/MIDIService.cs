using Commons.Music.Midi;
// using CoreMidi;

public class MIDIService
{
    public static MIDIService Instance => instance;
    private static readonly MIDIService instance = new MIDIService();

    private IMidiAccess2 _midiAccess { get; set; }
    private IMidiPortDetails _outputDevice { get; set; }
    private IMidiOutput? _midiOutput { get; set; }

    static MIDIService() { }
    
    private MIDIService()
    {
        // var client = new MidiClient("MyMIDIClient");
        // var endpoint = client.CreateVirtualSource("MyVirtualMIDISource", out var statusCode);

        _midiAccess = MidiAccessManager.Default as IMidiAccess2 ?? throw new Exception("Could not find default MIDIAccess");

        Console.WriteLine(string.Join(Environment.NewLine, _midiAccess.Outputs.Select(x => x.Name)));

        // // TODO: THIS NEEDS TO BE SET THROUGH THE UI (and or through a virtual MIDI port) at the moment its sort of hardwired to what ever hardware synth I enable
        _outputDevice = _midiAccess.Outputs.ElementAtOrDefault(1) ?? throw new Exception("No MIDI Device found");

        Console.WriteLine(string.Join(Environment.NewLine, _midiAccess.Outputs.Select(x => x.Name)));
    }

    public void SendMidiNoteOn(int note)
    {
        // Console.WriteLine($"Note On: {note}");

        if (_outputDevice == null) return;
        _midiOutput?.Send(new byte[] { 0x90, (byte)note, 0x7F }, 0, 3, 0);
    }

    public void SendMidiNoteOff(int note)
    {
        // Console.WriteLine($"Note Off: {note}");

        if (_outputDevice == null) return;
        _midiOutput?.Send(new byte[] { 0x80, (byte)note, 0x7F }, 0, 3, 0);
    }

    public void KillAllNotes()
    {
        for (int i = 0; i < 128; i++)
            SendMidiNoteOff(i);
    }

    public async Task OpenOutput()
    {
        _midiOutput = await _midiAccess.OpenOutputAsync(_outputDevice.Id);
    }
}