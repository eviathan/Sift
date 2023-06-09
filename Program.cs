using System;
using System.Threading;
using System.Collections.Generic;
using Sift.Logging;
using Sift.Sequencer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var sequence = new Sequence().Basic();
        var sequencer = new Sequencer(sequence);

        await MIDIService.Instance.OpenOutput();

        Logger.WriteBanner("Sift v0.0.1");
        Console.WriteLine("Press 'Q' to quit, 'Spacebar' to start/stop the sequence.");

        while(true)
        {
            if(args.Contains("--auto"))
            {
                sequencer.Play();
            }
            else
            {
                // NOTE: when we need to handle more input events 
                // move this into a temporary abstraction
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Quitting the application...");
                        MIDIService.Instance.KillAllNotes();
                        break;
                    }
                    else if(key == ConsoleKey.Spacebar)
                    {
                        if(sequencer.IsPlaying) sequencer.Stop();
                        else sequencer.Play();
                    }
                }
            }
        }
    }
}