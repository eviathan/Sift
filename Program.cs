using System;
using System.Threading;
using System.Collections.Generic;

public class MidiMessage
{
    public int Note { get; set; }
}

public class Event
{
    public int Timestamp { get; set; }
    public MidiMessage Message { get; set; }
}

public class Sequencer
{
    private List<Event> events;
    private int currentEvent = 0;
    private DateTime startTime;
    private bool isPlaying = false;

    public Sequencer(List<Event> events)
    {
        this.events = events;
        this.events.Sort((e1, e2) => e1.Timestamp.CompareTo(e2.Timestamp));
    }

    public void Play()
    {
        isPlaying = true;
        startTime = DateTime.Now;
        
        new Thread(() =>
        {
            while (isPlaying && currentEvent < events.Count)
            {
                var currentTimestamp = (DateTime.Now - startTime).TotalMilliseconds;

                if (currentTimestamp >= events[currentEvent].Timestamp)
                {
                    Console.WriteLine($"Playing note: {events[currentEvent].Message.Note}");
                    currentEvent++;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            
            currentEvent = 0;
        }).Start();
    }

    public void Stop()
    {
        isPlaying = false;
    }
}

public class Program
{
    public static void Main()
    {
        var events = new List<Event>
        {
            new Event { Timestamp = 0, Message = new MidiMessage { Note = 60 } },    // C
            new Event { Timestamp = 500, Message = new MidiMessage { Note = 62 } },  // D
            new Event { Timestamp = 1000, Message = new MidiMessage { Note = 64 } }, // E
        };

        var sequencer = new Sequencer(events);
        sequencer.Play();

        Thread.Sleep(5000);
        sequencer.Stop();
    }
}