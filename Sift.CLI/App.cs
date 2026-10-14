using Sift.Logging;
using Sift.Core;
using Sift.Services;
using Sift.Core.MIDI;
using Microsoft.Extensions.Logging;

namespace Sift.CLI
{
    public class App
    {
        private readonly ILogger<App> _logger;

        public bool IsRunning = true;
        public readonly Sequence Sequence;
        public readonly Sequencer Sequencer;
        public readonly IKeyEventService<ConsoleKey> KeyEventService;

        public App(ILogger<App> logger, Sequence sequence, Sequencer sequencer, IKeyEventService<ConsoleKey> keyEventService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            Sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            KeyEventService = keyEventService ?? throw new ArgumentNullException(nameof(keyEventService));
        }

        public async Task Init()
        {
            Logger.WriteBanner("Sift v0.0.1");
            _logger.LogInformation("Press 'Q' to quit, do'Spacebar' to start/stop the sequence.");

            RegisterKeyEvents();

            // TODO: Add this back when working with MIDI
            // await MIDIService.Instance.OpenOutput();
        }

        public void Update()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                KeyEventService.OnKeyDown(key);
            }
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
                _logger.LogInformation("Quitting the application...");
                
                MIDIService.Instance.KillAllNotes();
                IsRunning = false;
            });
        }
    }
}