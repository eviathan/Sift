using System.Diagnostics;
using Sift.Sequencer.MIDI;

namespace Sift.Sequencer
{
    public class SiftSequencer
    {
        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
        public int TickCount { get; private set; }

        private const int BUSY_WAITING_TIMEOUT_IN_MS = 1;

        private Sequence _sequence { get; set; }
        private Stopwatch _stopwatch { get; set; } = new();
        private Task _sequenceTask;
        private CancellationTokenSource _cancellationTokenSource { get; set; } = new();
        private MIDIService _midiService;

        public SiftSequencer(Sequence sequence)
        {
            _sequence = sequence;
            _sequenceTask = Task.Run(ExecuteSequence, _cancellationTokenSource.Token);
            _midiService = MIDIService.Instance;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _sequenceTask.Wait();
        }

        public void Play()
        {
            if (IsPlaying) return;

            IsPlaying = true;
            StartTime = DateTime.Now;

            _stopwatch.Reset();
            _stopwatch.Start();
        }

        public void Stop()
        {
            IsPlaying = false;
            TickCount = default;
            _stopwatch.Reset();
            _sequence.ResetWalker();
            MIDIService.Instance.KillAllNotes();
        }

        private void ExecuteSequence()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var noteHasElapased = _stopwatch.ElapsedMilliseconds >= BeatTimeInMs();

                if (IsPlaying && noteHasElapased)
                {
                    _sequence.Update(TickCount);
                    TickCount++;
                    _stopwatch.Restart();
                }
                else
                {
                    Thread.Sleep(BUSY_WAITING_TIMEOUT_IN_MS);
                }
            }
        }

        private double BeatTimeInMs()
        {
            var crotchetBeatLengthInMs = 60000.0 / _sequence.Tempo;
            double semiquaverLengthInMs = crotchetBeatLengthInMs / 4;
            return semiquaverLengthInMs;
        }
    }
}