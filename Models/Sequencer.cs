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

        private const int BUSY_WAITING_TIMEOUT  = 500; 

        private Sequence _sequence { get; set; }

        private Stopwatch _stopwatch { get; set; } = new Stopwatch();
        private Task _sequenceTask;
        private CancellationTokenSource _cancellationTokenSource { get; set; } = new CancellationTokenSource();

        private Stopwatch _debuggingStopwatch { get; set; } = new Stopwatch();

        public Sequencer(Sequence sequence)
        {
            Console.WriteLine($"0. Constructed Sequencer");
            _sequence = sequence;
            _sequenceTask = Task.Run(ExecuteSequence, _cancellationTokenSource.Token);

            _debuggingStopwatch.Start();
        }

        public void Dispose()
        {
            Console.WriteLine($"6: {_debuggingStopwatch.ElapsedMilliseconds}");
            _cancellationTokenSource.Cancel();
            _sequenceTask.Wait();
        }

        public void Play()
        {
            Console.WriteLine($"3: Hit Play {_debuggingStopwatch.ElapsedMilliseconds}");
            if(IsPlaying) return;
            
            Console.WriteLine($"4: {_debuggingStopwatch.ElapsedMilliseconds}");

            IsPlaying = true;
            StartTime = DateTime.Now;
            
            _stopwatch.Reset();
            _stopwatch.Start();
            
            Console.WriteLine($"5: {_debuggingStopwatch.ElapsedMilliseconds}");
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
            // NOTE: THIS THREAD GETS CREATED ALMOST IMMEDIATELY So this should be 0
            Console.WriteLine($"1. THREAD STARTED WHEN Application Creates Sequencer: {_debuggingStopwatch.ElapsedMilliseconds}");
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var noteHasElapased = _stopwatch.ElapsedMilliseconds >= BeatTimeInMs();

                if (IsPlaying && noteHasElapased)
                {
                    Console.WriteLine($"4: {_debuggingStopwatch.ElapsedMilliseconds}");
                    foreach (var tree in _sequence.Trees)
                        tree.Traverse();

                    Console.WriteLine($"5: {_debuggingStopwatch.ElapsedMilliseconds}");

                    BeatCount++;
                    _stopwatch.Restart();
                }
                else
                {
                    Thread.Sleep(BUSY_WAITING_TIMEOUT); 
                    Console.WriteLine($"2. THREAD SLEPT FOR 500ms: {_debuggingStopwatch.ElapsedMilliseconds}-IsPlaying:{IsPlaying}-noteHasElapased:{noteHasElapased}");
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