using Sift.Logging;
using Sift.Sequencer;
using Sift.Services;

namespace Sift
{
    public class App
    {
        public bool IsRunning = true;

        private readonly SiftSequencer _sequencer;
        private readonly KeyEventService _keyEventService;

        public App()
        {
            var sequence = new Sequence().Basic();
            _sequencer = new Sequencer.SiftSequencer(sequence);
            _keyEventService = new KeyEventService(_sequencer);

            RegisterKeyEvents();

            // await MIDIService.Instance.OpenOutput();

            Logger.WriteBanner("Sift v0.0.1");
            Console.WriteLine("Press 'Q' to quit, 'Spacebar' to start/stop the sequence.");
        }

        public void Update()
        {
            _keyEventService.Evaluate();
        }

        private void RegisterKeyEvents()
        {
            _keyEventService.RegisterKeyEvent(ConsoleKey.Spacebar, () => {
                if(_sequencer.IsPlaying)
                    _sequencer.Stop();
                else
                    _sequencer.Play();
            });

            _keyEventService.RegisterKeyEvent(ConsoleKey.Q, () => {
                Console.WriteLine("Quitting the application...");
                MIDIService.Instance.KillAllNotes();
                IsRunning = false;
            });
        }
    }
}