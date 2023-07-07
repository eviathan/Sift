using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sift.Sequencer;

namespace Sift.Services
{
    public class KeyEventService
    {
        private readonly Sequencer.SiftSequencer _sequencer;

        private Dictionary<ConsoleKey, Action> _keyEvents { get; set; } = new Dictionary<ConsoleKey, Action>();

        public KeyEventService(Sequencer.SiftSequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Evaluate()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if(_keyEvents.TryGetValue(key, out var action))
                {
                    action();
                }
            }
        }

        public void RegisterKeyEvent(ConsoleKey key, Action action)
        {
            _keyEvents[key] = action;
        }
    }
}