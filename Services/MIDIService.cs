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
        _outputDevice = _midiAccess.Outputs.ElementAt(1);
        
        Console.WriteLine("Devices:");
        Console.WriteLine(string.Join(Environment.NewLine, _midiAccess.Outputs.Select((x, i) => $"{i}: {x.Name}")));
        Console.WriteLine($"{Environment.NewLine}Selected Device: 1 {_outputDevice.Name}");
    }

    public void SendMidiNoteOn(int note)
    {
        if (_outputDevice == null)
        {
            Console.WriteLine("No MIDI output devices found.");
            return;
        }
        
        Console.WriteLine($"NoteOn: {note}");

        // TODO: This needs be determinned by the note length/ or a better way than just chewing through threads
        // var midiOutput = _midiAccess.OpenOutputAsync(_outputDevice.Id);
        _midiOutput?.Send(new byte[] { 0x90, (byte)note, 0x7F }, 0, 3, 0); // Note on
    }

    public void SendMidiNoteOff(int note)
    {
        if (_outputDevice == null)
        {
            Console.WriteLine("No MIDI output devices found.");
            return;
        }
        
        Console.WriteLine($"Note Off: {note}");
        _midiOutput?.Send(new byte[] { 0x80, (byte)note, 0x7F }, 0, 3, 0); // Note off
    }

    public async Task OpenOutput()
    {
        _midiOutput = await _midiAccess.OpenOutputAsync(_outputDevice.Id);
    }
}