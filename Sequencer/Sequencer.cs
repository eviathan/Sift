using System.Diagnostics;

namespace Sift.Sequencer
{
    public class Sequencer
    {
        private const int BUSY_WAITING_TIMEOUT_IN_MS = 1; 

        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
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
            if(IsPlaying) 
            {
                Stop();
                return;
            }

            IsPlaying = true;
            StartTime = DateTime.Now;
            
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        public void Stop()
        {
            IsPlaying = false;

            _stopwatch.Reset();
            _sequence.ResetTrees();

            MIDIService.Instance.KillAllNotes();
        }

        private void ExecuteSequence()
        {
            var lastUpdateTime = 0L;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (IsPlaying)
                {
                    foreach (var tree in _sequence.Trees)
                    {
                        var currentTime = _stopwatch.ElapsedMilliseconds;

                        var context = new SequencerContext
                        {
                            Time = _stopwatch.ElapsedMilliseconds,
                            DeltaTime = currentTime - lastUpdateTime,
                        };

                        tree.Update(context);
                        lastUpdateTime = currentTime;
                    }
                }
                else
                {
                    Thread.Sleep(BUSY_WAITING_TIMEOUT_IN_MS);
                }
            }
        }
    }
}