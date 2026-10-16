using System.Diagnostics;
using Sift.Core.Interfaces;

namespace Sift.Core
{
    // NOTE: The wall clock. Converts elapsed time into ticks at the current tempo and drives the SequencerRuntime
    public class Sequencer : IDisposable
    {
        private const int IDLE_SLEEP_IN_MS = 1;

        // NOTE: Sleep while the next event is further away than this, spin when it's closer. Thread.Sleep(1) can
        // oversleep by a millisecond or more, spinning keeps events sub-millisecond accurate
        private const double SPIN_THRESHOLD_IN_MS = 2.0;

        private readonly Sequence _sequence;
        private readonly SequencerRuntime _runtime;
        private readonly IMidiOutput _midiOutput;

        // NOTE: Guards Play/Stop and the clock's position so a restart can never be advanced with a stale position
        private readonly object _sync = new();
        private readonly Thread _clockThread;
        private volatile bool _isDisposed;
        private volatile bool _isPlaying;

        private double _position;
        private long _lastTimestamp;

        public bool IsPlaying => _isPlaying;

        // NOTE: Fractional ticks since Play, for drawing a smooth playhead
        public double Position => Volatile.Read(ref _position);

        public double TicksPerSecond => _sequence.Tempo / 60.0 * Time.TicksPerQuarterNote;

        public Sequencer(Sequence sequence, SequencerRuntime runtime, IMidiOutput midiOutput)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

            _midiOutput = midiOutput ?? throw new ArgumentNullException(nameof(midiOutput));

            _clockThread = new Thread(RunClock)
            {
                Name = "Sift Clock",
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            _clockThread.Start();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();

            _isDisposed = true;
            _clockThread.Join();
        }

        public void Play()
        {
            lock (_sync)
            {
                if (_isPlaying || _isDisposed)
                    return;

                _runtime.Start();

                Volatile.Write(ref _position, 0);
                _lastTimestamp = Stopwatch.GetTimestamp();
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
                _runtime.Stop();

                Volatile.Write(ref _position, 0);
            }

            // NOTE: Cancelled MIDI components send their own note offs, this catches anything left hanging
            _midiOutput.AllNotesOff();
        }

        private void RunClock()
        {
            while (!_isDisposed)
            {
                if (!_isPlaying)
                {
                    Thread.Sleep(IDLE_SLEEP_IN_MS);
                    continue;
                }

                double position;
                double ticksPerSecond;

                lock (_sync)
                {
                    if (!_isPlaying)
                        continue;

                    // NOTE: Accumulate rather than derive from the start time so tempo changes don't jump the playhead
                    var now = Stopwatch.GetTimestamp();
                    ticksPerSecond = TicksPerSecond;
                    position = _position + Stopwatch.GetElapsedTime(_lastTimestamp, now).TotalSeconds * ticksPerSecond;
                    _lastTimestamp = now;

                    Volatile.Write(ref _position, position);
                    _runtime.AdvanceTo(new Time((long)position));
                }

                WaitForNextTick(position, ticksPerSecond);
            }
        }

        private void WaitForNextTick(double position, double ticksPerSecond)
        {
            var nextDueTime = _runtime.NextDueTime;

            var msUntilNextDue = nextDueTime is { } dueTime && ticksPerSecond > 0
                ? (dueTime.Ticks - position) / ticksPerSecond * 1000
                : double.MaxValue;

            if (msUntilNextDue > SPIN_THRESHOLD_IN_MS)
                Thread.Sleep(IDLE_SLEEP_IN_MS);
            else
                Thread.SpinWait(50);
        }
    }
}
