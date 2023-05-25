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

        private CancellationTokenSource? _cancellationTokenSource { get; set; }
        private Sequence _sequence { get; set; }

        public Sequencer(Sequence sequence)
        {
            _sequence = sequence;
        }

        public async Task Play()
        {
            if(IsPlaying) return;

            IsPlaying = true;
            StartTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();

            await Task.Run(() => ExecuteSequence(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            IsPlaying = false;
            _cancellationTokenSource?.Cancel();
        }

        private void ExecuteSequence(CancellationToken cancellationToken)
        {
            while (IsPlaying)
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