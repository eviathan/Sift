using Sift.Logging;
using Sift.Core;
using Sift.Services;
using Microsoft.Extensions.Logging;
using Sift.Core.Factories;

namespace Sift.CLI
{
    public class App
    {
        private readonly ILogger<App> _logger;

        public bool IsRunning = true;
        public readonly Sequence Sequence;
        public readonly Sequencer Sequencer;
        public readonly IKeyEventService<ConsoleKey> KeyEventService;

        private readonly ComponentFactory _componentFactory;

        public App(ILogger<App> logger, Sequence sequence, Sequencer sequencer, IKeyEventService<ConsoleKey> keyEventService, ComponentFactory componentFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            Sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
            KeyEventService = keyEventService ?? throw new ArgumentNullException(nameof(keyEventService));
            _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        }

        public async Task Init()
        {
            Logger.WriteBanner("Sift v0.0.1");
            _logger.LogInformation("Press 'Q' to quit, do'Spacebar' to start/stop the sequence.");

            RegisterKeyEvents();

            DemoScene.Build(Sequence, _componentFactory);
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
                    Sequencer.Stop();
                else
                    Sequencer.Play();
            });

            KeyEventService.RegisterKeyEvent(ConsoleKey.Q, () =>
            {
                _logger.LogInformation("Quitting the application...");
                
                Sequencer.Dispose();
                IsRunning = false;
            });
        }
    }
}