using Sift.Logging;
using Sift.Sequencer;
using Sift.Services;
using Sift.Sequencer.MIDI;

namespace Sift.CLI
{
    public class App
    {
        public bool IsRunning = true;
        public readonly Sequence Sequence;
        public readonly SiftSequencer Sequencer;
        public readonly KeyEventService KeyEventService;

        public App()
        {
            Sequence = new Sequence();
            Sequencer = new SiftSequencer(Sequence);

            KeyEventService = new KeyEventService(Sequencer);
        }

        public async Task Init()
        {
            Logger.WriteBanner("Sift v0.0.1");
            Console.WriteLine("Press 'Q' to quit, do'Spacebar' to start/stop the sequence.");

            RegisterKeyEvents();

            // TODO: Add this back when working with MIDI
            // await MIDIService.Instance.OpenOutput();
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