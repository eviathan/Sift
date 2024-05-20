using Sift.Logging;
using Sift.Sequencer;
using Sift.Services;
using Sift.Sequencer.MIDI;
using Sift.Sequencer.Extensions;

namespace Sift
{
    public class App
    {
        public bool IsRunning = true;
        public readonly Sequence Sequence;
        public readonly SiftSequencer Sequencer;
        public readonly KeyEventService KeyEventService;
        public readonly bool IsDebug;

        public App(string[] args)
        {
            IsDebug = args.Contains("--vscode");

            Sequence = new Sequence();
            Sequencer = new SiftSequencer(Sequence);
            KeyEventService = new KeyEventService(Sequencer);
        }

        public async Task Init()
        {
            Logger.WriteBanner("Sift v0.0.1");
            Console.WriteLine("Press 'Q' to quit, 'Spacebar' to start/stop the sequence.");

            RegisterKeyEvents();
            await MIDIService.Instance.OpenOutput();
        }

        public void Update()
        {
            KeyEventService.Evaluate();
        }

        private void RegisterKeyEvents()
        {
            KeyEventService.RegisterKeyEvent(ConsoleKey.Spacebar, () =>
            {
                if (Sequencer.IsPlaying)
                {
                    Sequence.Deinitialise();
                    Sequencer.Stop();
                }
                else
                {
                    Sequence.Initialise();
                    Sequencer.Play();
                }
            });

            KeyEventService.RegisterKeyEvent(ConsoleKey.Q, () =>
            {
                Console.WriteLine("Quitting the application...");
                MIDIService.Instance.KillAllNotes();
                IsRunning = false;
            });
        }
    }
}