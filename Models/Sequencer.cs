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

        private Sequence _sequence { get; set; }
        private Stopwatch _stopwatch { get; set; } = new Stopwatch();

        public Sequencer(Sequence sequence)
        {
            _sequence = sequence;
        }

        public void Play()
        {
            if(IsPlaying) return;

            IsPlaying = true;
            StartTime = DateTime.Now;

            _stopwatch.Start();

            var sequenceThread = new Thread(ExecuteSequence) 
            { 
                IsBackground = true
            };
            
            sequenceThread.Start();
        }

        public void Stop()
        {
            IsPlaying = false;
            BeatCount = default;
            _stopwatch.Reset();
            _sequence.ResetTrees();
        }

        private void ExecuteSequence()
        {
            while (IsPlaying)
            {
                if (_stopwatch.ElapsedMilliseconds >= BeatTimeInMs())
                {
                    foreach (var tree in _sequence.Trees)
                        tree.Traverse();

                    BeatCount++;
                    _stopwatch.Restart();
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