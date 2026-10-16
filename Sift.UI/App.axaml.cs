using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Sift.Logging;
using Sift.Core;
using Sift.Services;
using Sift.UI.Interfaces;
using Sift.UI.ViewModels;
using Sift.UI.Views;

namespace Sift.UI
{
    // TODO: Add MIDI Service initialisation
    public partial class App : Application
    {
        public bool IsRunning = true;
        public Sequence Sequence { get; private set; }
        public Sequencer Sequencer { get; private set; }
        public IKeyEventService<Key> KeyEventService { get; private set; }

        public override void Initialize()
        {
            Logger.WriteBanner("Sift v0.0.1");

            // Sequence = new Sequence();
            // Sequencer = new Sequencer(Sequence);
            KeyEventService = new AvaloniaKeyEventService(Sequencer);

            AvaloniaXamlLoader.Load(this);

            RegisterKeyEvents();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void RegisterKeyEvents()
        {
            KeyEventService.RegisterKeyEvent(Key.Space, () =>
            {
                if (Sequencer.IsPlaying)
                    Sequencer.Stop();
                else
                    Sequencer.Play();
            });
        }
    }
}