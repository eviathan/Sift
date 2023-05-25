using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Models
{
    public class Sequencer
    {
        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
        public int BeatCount { get; private set; }

        private Sequence _sequence { get; set; }
        private CancellationTokenSource? _cancellationTokenSource { get; set; }

        public Sequencer(Sequence sequence)
        {
            _sequence = sequence;
        }

        public void Play()
        {
            if(IsPlaying) return;

            IsPlaying = true;
            StartTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => ExecuteSequence(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            IsPlaying = false;
            BeatCount = default;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void ExecuteSequence(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsPlaying)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentTimestamp = (DateTime.Now - StartTime).TotalMilliseconds;

                if (currentTimestamp >= NextBeatTimeInMs())
                {
                    Console.WriteLine($"Beat Count: {BeatCount}");

                    BeatCount++;
                }
            }
        }

        private double NextBeatTimeInMs() 
        {
            var crotchetBeatLengthInMs = 60000.0 / _sequence.Tempo;
            double semiquaverLengthInMs = crotchetBeatLengthInMs / 4;
            return semiquaverLengthInMs * (BeatCount + 1);
        }
    }
}