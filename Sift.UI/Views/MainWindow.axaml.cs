using Avalonia.Controls;
using Avalonia.Input;

namespace Sift.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly App _app;

        public MainWindow()
        {
            InitializeComponent();
            _app = Avalonia.Application.Current as App;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _app.KeyEventService.OnKeyDown(e.Key);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _app.KeyEventService.OnKeyUp(e.Key);
        }
    }
}