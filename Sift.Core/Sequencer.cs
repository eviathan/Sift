using System.Diagnostics;
using Sift.Core.MIDI;

namespace Sift.Core
{
    public class Sequencer : IDisposable
    {
        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
        public UInt64 TickCount { get; private set; }

        private const int BUSY_WAITING_TIMEOUT_IN_MS = 1;
        private const int TICKS_PER_CROTCHET = 96;

        private Sequence _sequence { get; set; }
        private Stopwatch _stopwatch { get; set; } = new();
        private Task _sequenceTask;
        private CancellationTokenSource _cancellationTokenSource = new();
        private MIDIService _midiService;

        public Sequencer(Sequence sequence)
        {
            _sequence = sequence;
            _midiService = MIDIService.Instance;
            _sequenceTask = Task.Run(ExecuteSequence, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _sequenceTask.Wait();
            _cancellationTokenSource.Dispose();
        }

        public void Play()
        {
            if (IsPlaying)
                return;

            IsPlaying = true;
            StartTime = DateTime.Now;

            _stopwatch.Restart();
        }

        public void Stop()
        {
            IsPlaying = false;
            TickCount = 0;
            _stopwatch.Reset();

            _midiService.KillAllNotes();
        }

        private void ExecuteSequence()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (!IsPlaying)
                {
                    Thread.Sleep(BUSY_WAITING_TIMEOUT_IN_MS);
                    continue;
                }

                var crotchetLengthInMilliseconds = 60000.0 / _sequence.Tempo;

                var targetTick = (UInt64)(_stopwatch.Elapsed.TotalMilliseconds / crotchetLengthInMilliseconds * TICKS_PER_CROTCHET);

                while (IsPlaying && TickCount < targetTick)
                {
                    _sequence.Update(TickCount);
                    TickCount++;
                }

                Thread.Sleep(BUSY_WAITING_TIMEOUT_IN_MS);
            }
        }
    }
}