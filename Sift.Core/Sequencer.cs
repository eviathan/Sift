using System.Diagnostics;
using Sift.Core.MIDI;
using Sift.Core.Models.Nodes;

namespace Sift.Core
{
    public class Sequencer : IDisposable
    {
        private const int BUSY_WAITING_TIMEOUT_IN_MS = 1;
        private const int TICKS_PER_CROTCHET = 96;

        private readonly EventScheduler _eventScheduler;

        private Sequence _sequence { get; set; }
        private Stopwatch _stopwatch { get; set; } = new();
        private Task _sequenceTask;
        private CancellationTokenSource _cancellationTokenSource = new();
        private MIDIService _midiService;
        public bool IsPlaying { get; private set; }
        public DateTime StartTime { get; private set; }
        public UInt64 TickCount { get; private set; }


        public Sequencer(Sequence sequence, EventScheduler eventScheduler)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _eventScheduler = eventScheduler ?? throw new ArgumentNullException(nameof(eventScheduler));

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

            BuildEventSchedule();

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

                while (IsPlaying)
                { 
                    _sequence.Update(TickCount);
                    TickCount++;
                }

                Thread.Sleep(BUSY_WAITING_TIMEOUT_IN_MS);
            }
        }

        private void BuildEventSchedule()
        {
            // _eventScheduler.Schedule(new NodeEvent
            // { 
            //     Id = Guid.NewGuid(),
            //     DueTime = 100
            // });
        }
    }
}