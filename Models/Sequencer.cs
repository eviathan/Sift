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
            _sequence.ResetTrees();
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
                    foreach (var tree in _sequence.Trees)
                        tree.Traverse();

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

// NOTE: Use this to improve timing and reduce the unecessary thread overhead.
// On each traversal:
// - Send MIDI Note On to current TreeNode event and MIDI Note Off to the previous

// using System.Diagnostics;

// private readonly int ticksPerSemiquaver;
// ticksPerSemiquaver = (int)(Stopwatch.Frequency * 60.0 / bpm / 4);

// public void Start()
//     {
//         stopwatch.Start();
//         new Thread(ProcessEvents) { IsBackground = true }.Start();
//     }

//     private void ProcessEvents()
//     {
//         while (true)
//         {
//             if (stopwatch.ElapsedTicks >= tree.CurrentNode.Timestamp * ticksPerSemiquaver)
//             {
//                 tree.Traverse();
//                 stopwatch.Restart();
//             }
//         }
//     }