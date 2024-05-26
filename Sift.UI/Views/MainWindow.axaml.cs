using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Sift.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        // Handle key up event
        if (e.Key == Key.Space)
        {
            // Do something when 'A' key is pressed
            Console.WriteLine("Spacebar hit");
            var app = Avalonia.Application.Current as App;
            var sequence = app.Sequence;
            var sequencer = app.Sequencer;

            if (sequencer.IsPlaying)
            {
                sequence.Deinitialise();
                sequencer.Stop();
            }
            else
            {
                sequence.Initialise();
                sequencer.Play();
            }
        }
    }
}
