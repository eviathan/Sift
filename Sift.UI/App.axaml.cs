using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Sift.Logging;
using Sift.Sequencer;
using Sift.UI.ViewModels;
using Sift.UI.Views;

namespace Sift.UI
{
    // TODO: Add MIDI Service initialisation
    public partial class App : Application
    {
        public bool IsRunning = true;
        public Sequence Sequence { get; private set; }
        public SiftSequencer Sequencer { get; private set; }

        public override void Initialize()
        {
            Logger.WriteBanner("Sift v0.0.1");

            Sequence = new Sequence();
            Sequencer = new SiftSequencer(Sequence);

            AvaloniaXamlLoader.Load(this);
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
    }
}