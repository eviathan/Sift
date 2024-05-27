using System;
using System.Collections.Generic;
using Avalonia.Input;
using Sift.Sequencer;
using Sift.Services;

namespace Sift.UI.Interfaces
{
    public class AvaloniaKeyEventService : IKeyEventService<Key>
    {
        public Dictionary<Key, Action> KeyEvents { get; set; } = [];
        private SiftSequencer _sequencer { get; set; }
        private HashSet<Key> _currentKeyEvents { get; set; } = [];

        public AvaloniaKeyEventService(SiftSequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void RegisterKeyEvent(Key key, Action action)
        {
            KeyEvents[key] = action;
        }

        public void OnKeyDown(Key key = default)
        {
            if (key != default &&
                !_currentKeyEvents.Contains(key) &&
                KeyEvents.TryGetValue(key, out var action))
            {
                action();
                _currentKeyEvents.Add(key);
            }
        }

        public void OnKeyUp(Key key = default)
        {
            _currentKeyEvents.Remove(key);
        }
    }
}