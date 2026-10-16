using System.Diagnostics;
using Sift.Core.MIDI;

namespace Sift.Core
{
    // NOTE: The wall clock. Converts elapsed time into ticks and drives the SequencerRuntime
    // TODO: Phase 4, accumulate ticks so tempo changes during playback don't jump the playhead
    public class Sequencer : IDisposable
    {
        private const int CLOCK_INTERVAL_IN_MS = 1;

        private readonly Sequence _sequence;
        private readonly SequencerRuntime _runtime;
        private readonly MIDIService _midiService;

        private readonly Stopwatch _stopwatch = new();
        // NOTE: Keeps Play/Stop atomic with the stopwatch
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _clockTask;
        private volatile bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public Sequencer(Sequence sequence, SequencerRuntime runtime)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

            _midiService = MIDIService.Instance;
            _clockTask = Task.Run(RunClock, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            Stop();

            _cancellationTokenSource.Cancel();
            _clockTask.Wait();
            _cancellationTokenSource.Dispose();
        }

        public void Play()
        {
            lock (_sync)
            {
                if (_isPlaying)
                    return;

                _runtime.Start();
                _stopwatch.Restart();
                _isPlaying = true;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!_isPlaying)
                    return;

                _isPlaying = false;
                _stopwatch.Reset();
                _runtime.Stop();
            }

            _midiService.KillAllNotes();
        }

        private void RunClock()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // NOTE: The runtime does its own locking and ignores this once stopped
                if (_isPlaying)
                    _runtime.AdvanceTo(GetCurrentTick());

                Thread.Sleep(CLOCK_INTERVAL_IN_MS);
            }
        }

        private Time GetCurrentTick() =>
            new((long)(_stopwatch.Elapsed.TotalMinutes * _sequence.Tempo * Time.TicksPerQuarterNote));
    }
}
