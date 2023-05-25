using System;
using System.Threading;
using System.Collections.Generic;
using Sift.Models;
using Sift.Logging;

public class Program
{
    public static async Task Main()
    {
        var sequence = new Sequence();
        var sequencer = new Sequencer(sequence);

        Logger.WriteBanner("Sift v0.0.1");
        Console.WriteLine("Press 'Q' to quit, 'Spacebar' to start/stop the sequence.");

        while(true)
        {
            // NOTE: when we need to handle move this into an abstraction
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Q)
                {
                    Console.WriteLine("Quitting the application...");
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

#region Reference
// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;

// public class MidiMessage
// {
//     public int Note { get; set; }
// }

// public class Event
// {
//     public int Timestamp { get; set; }
//     public MidiMessage Message { get; set; }
// }

// public class Sequencer
// {
//     private List<Event> events;
//     private int currentEvent = 0;
//     private DateTime startTime;
//     private CancellationTokenSource cancellationTokenSource;

//     public bool IsPlaying { get; private set; } = false;

//     public Sequencer(List<Event> events)
//     {
//         this.events = events;
//         this.events.Sort((e1, e2) => e1.Timestamp.CompareTo(e2.Timestamp));
//     }

//     public async Task Play()
//     {
//         if (!IsPlaying)
//         {
//             IsPlaying = true;
//             startTime = DateTime.Now;
//             cancellationTokenSource = new CancellationTokenSource();

//             await Task.Run(() => SequencerTask(cancellationTokenSource.Token));
//         }
//     }

//     private void SequencerTask(CancellationToken cancellationToken)
//     {
//         while (IsPlaying && currentEvent < events.Count)
//         {
//             cancellationToken.ThrowIfCancellationRequested();

//             var currentTimestamp = (DateTime.Now - startTime).TotalMilliseconds;

//             if (currentTimestamp >= events[currentEvent].Timestamp)
//             {
//                 Console.WriteLine($"Playing note: {events[currentEvent].Message.Note}");
//                 currentEvent++;
//             }
//             else
//             {
//                 Thread.Sleep(1);
//             }
//         }

//         if (IsPlaying)
//         {
//             IsPlaying = false;
//             currentEvent = 0;
//         }
//     }

//     public void Stop()
//     {
//         if (IsPlaying)
//         {
//             IsPlaying = false;
//             cancellationTokenSource.Cancel();
//         }
//     }
// }

// public class Program
// {
//     public static async Task Main()
//     {
//         var events = new List<Event>
//         {
//             new Event { Timestamp = 0, Message = new MidiMessage { Note = 60 } },    // C
//             new Event { Timestamp = 500, Message = new MidiMessage { Note = 62 } },  // D
//             new Event { Timestamp = 1000, Message = new MidiMessage { Note = 64 } }, // E
//         };

//         var sequencer = new Sequencer(events);

//         Console.WriteLine("Press 'Q' to quit, 'Spacebar' to start/stop the sequence.");

//         while (true)
//         {
//             if (Console.KeyAvailable)
//             {
//                 var key = Console.ReadKey(true).Key;

//                 if (key == ConsoleKey.Q)
//                 {
//                     if (sequencer.IsPlaying)
//                     {
//                         sequencer.Stop();
//                     }
//                     Console.WriteLine("Quitting the application...");
//                     break;
//                 }
//                 else if (key == ConsoleKey.Spacebar)
//                 {
//                     if (sequencer.IsPlaying)
//                     {
//                         sequencer.Stop();
//                     }
//                     else
//                     {
//                         await sequencer.Play();
//                     }
//                 }
//             }
//         }
//     }
// }
#endregion