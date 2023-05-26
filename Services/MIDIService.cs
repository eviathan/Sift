using Commons.Music.Midi;

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
        _midiAccess = MidiAccessManager.Default as IMidiAccess2 ?? throw new Exception("Could not find default MIDIAccess");

         // TODO: THIS NEEDS TO BE SET THROUGH THE UI (and or through a virtual MIDI port) at the moment its sort of hardwired to what ever hardware synth I enable
        _outputDevice = _midiAccess.Outputs.ElementAtOrDefault(1) ?? throw new Exception("No MIDI Device found");
    }

    public void SendMidiNoteOn(int note)
    {
        if (_outputDevice == null) return;
        _midiOutput?.Send(new byte[] { 0x90, (byte)note, 0x7F }, 0, 3, 0);
    }

    public void SendMidiNoteOff(int note)
    {
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