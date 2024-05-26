using Sift.Services;

namespace Sift.CLI.Services
{
    public class ConsoleKeyEventService : IKeyEventService<ConsoleKey>
    {
        private readonly Sequencer.SiftSequencer _sequencer;

        public Dictionary<ConsoleKey, Action> KeyEvents { get; set; } = [];

        public ConsoleKeyEventService(Sequencer.SiftSequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void RegisterKeyEvent(ConsoleKey key, Action action)
        {
            KeyEvents[key] = action;
        }

        public void OnKeyDown(ConsoleKey key = default)
        {
            if (key != default && KeyEvents.TryGetValue(key, out var action))
                action();
        }

        public void OnKeyUp(ConsoleKey key = default)
        {
            throw new NotImplementedException();
        }
    }
}