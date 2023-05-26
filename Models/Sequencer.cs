using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Sift.Models
{
    public class Sequencer
    {
        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
        public int BeatCount { get; private set; }

        private const int BUSY_WAITING_TIMEOUT_IN_MS = 1; 

        private Sequence _sequence { get; set; }

        private Stopwatch _stopwatch { get; set; } = new Stopwatch();
        private Task _sequenceTask;
        private CancellationTokenSource _cancellationTokenSource { get; set; } = new CancellationTokenSource();

        public Sequencer(Sequence sequence)
        {
            _sequence = sequence;
            _sequenceTask = Task.Run(ExecuteSequence, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _sequenceTask.Wait();
        }

        public void Play()
        {
            if(IsPlaying) return;

            IsPlaying = true;
            StartTime = DateTime.Now;
            
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        public void Stop()
        {
            IsPlaying = false;
            BeatCount = default;
            _stopwatch.Reset();
            _sequence.ResetTrees();
            MIDIService.Instance.KillAllNotes();
        }

        private void ExecuteSequence()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var noteHasElapased = _stopwatch.ElapsedMilliseconds >= BeatTimeInMs();

                if (IsPlaying && noteHasElapased)
                {
                    foreach (var tree in _sequence.Trees)
                        tree.Traverse();

                    BeatCount++;
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