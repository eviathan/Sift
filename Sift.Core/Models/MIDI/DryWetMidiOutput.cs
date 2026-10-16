using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Logging;
using Sift.Core.Interfaces;

namespace Sift.Core.MIDI
{
    // NOTE: Sends to a virtual MIDI device that DAWs can listen to. If the device can't be created (e.g. virtual
    // devices aren't supported on this platform) everything becomes a no-op rather than taking playback down
    public class DryWetMidiOutput : IMidiOutput, IDisposable
    {
        private const string MIDI_DEVICE_NAME = "Sift MIDI";
        private const int ALL_NOTES_OFF_CONTROLLER = 123;

        private readonly ILogger<DryWetMidiOutput> _logger;
        private readonly object _sync = new();

        // NOTE: Counts overlapping notes so one component's note off doesn't cut another's still sounding note
        private readonly Dictionary<(int Channel, int Note), int> _soundingNotes = [];

        private VirtualDevice? _virtualDevice;
        private OutputDevice? _outputDevice;

        public bool IsAvailable => _outputDevice != null;

        public DryWetMidiOutput(ILogger<DryWetMidiOutput> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _virtualDevice = VirtualDevice.Create(MIDI_DEVICE_NAME);
                _outputDevice = OutputDevice.GetByName(MIDI_DEVICE_NAME);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"MIDI output '{MIDI_DEVICE_NAME}' is unavailable, MIDI will not be sent: {ex.Message}");
            }
        }

        public void NoteOn(int channel, int note, int velocity)
        {
            lock (_sync)
            {
                var key = (channel, note);
                _soundingNotes[key] = _soundingNotes.GetValueOrDefault(key) + 1;

                Send(new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity) { Channel = ToChannel(channel) });
            }
        }

        public void NoteOff(int channel, int note)
        {
            lock (_sync)
            {
                var key = (channel, note);

                if (!_soundingNotes.TryGetValue(key, out var count))
                    return;

                if (count > 1)
                {
                    _soundingNotes[key] = count - 1;
                    return;
                }

                _soundingNotes.Remove(key);
                Send(new NoteOffEvent((SevenBitNumber)note, SevenBitNumber.MinValue) { Channel = ToChannel(channel) });
            }
        }

        public void AllNotesOff()
        {
            lock (_sync)
            {
                foreach (var (channel, note) in _soundingNotes.Keys)
                    Send(new NoteOffEvent((SevenBitNumber)note, SevenBitNumber.MinValue) { Channel = ToChannel(channel) });

                _soundingNotes.Clear();

                // NOTE: Belt and braces for anything we didn't track (e.g. a note on from before a crash)
                for (var channel = 1; channel <= 16; channel++)
                    Send(new ControlChangeEvent((SevenBitNumber)ALL_NOTES_OFF_CONTROLLER, SevenBitNumber.MinValue) { Channel = ToChannel(channel) });
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _outputDevice?.Dispose();
                _virtualDevice?.Dispose();
                _outputDevice = null;
                _virtualDevice = null;
            }
        }

        private void Send(MidiEvent midiEvent)
        {
            if (_outputDevice == null)
                return;

            try
            {
                _outputDevice.SendEvent(midiEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send MIDI event {midiEvent}.");
            }
        }

        private static FourBitNumber ToChannel(int channel) =>
            (FourBitNumber)(channel - 1);
    }
}
