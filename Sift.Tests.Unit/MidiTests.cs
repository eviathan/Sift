using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Logging.Abstractions;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.MIDI;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    public class MidiTests
    {
        [Fact]
        public void A_note_is_held_for_its_duration_on_its_channel()
        {
            using var sift = new SiftFixture();
            sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Node(new(1, 0, 0), "Note", Direction.Left, new MidiConfig(Note: 64, Velocity: 90, Channel: 3, Duration: NoteDuration.EighthNote));

            sift.Runtime.Start();
            sift.Play(200);

            // NOTE: The note off is stamped with the clock when it runs on the thread pool, which can be a tick or two later
            var events = sift.Midi.Events;
            Assert.Equal(2, events.Count);
            Assert.Equal("on ch3 n64 v90 @96", events[0]);
            Assert.StartsWith("off ch3 n64 @", events[1]);
            Assert.InRange(long.Parse(events[1].Split('@')[1]), 144, 150);
        }

        [Fact]
        public void Stopping_sends_the_held_note_off_then_all_notes_off()
        {
            using var sift = new SiftFixture();
            sift.Sequence.Tempo = 240;
            sift.Source(new(0, 0, 0), Direction.Up, new MidiConfig(Duration: NoteDuration.WholeNote));
            var sequencer = sift.CreateSequencer();

            sequencer.Play();
            Thread.Sleep(150);
            sequencer.Stop();

            var events = sift.Midi.Events;
            Assert.Equal(3, events.Count);
            Assert.StartsWith("on ch1 n60", events[0]);
            Assert.StartsWith("off ch1 n60", events[1]);
            Assert.Equal("all-off", events[2]);
        }

        // NOTE: Listens on the real virtual device, so only this test's channels count
        [DeviceFact]
        public void The_virtual_device_counts_overlapping_notes_and_clears_everything_on_all_notes_off()
        {
            using var output = new DryWetMidiOutput(NullLogger<DryWetMidiOutput>.Instance);
            Assert.True(output.IsAvailable);

            var received = new System.Collections.Concurrent.ConcurrentQueue<string>();
            using var input = InputDevice.GetByName("Sift MIDI");

            input.EventReceived += (_, e) =>
            {
                if (e.Event is ChannelEvent channelEvent && channelEvent.Channel + 1 is not 2 and not 5 && e.Event is not ControlChangeEvent)
                    return;

                received.Enqueue(e.Event switch
                {
                    NoteOnEvent on => $"on{on.Channel + 1}:{on.NoteNumber}:{on.Velocity}",
                    NoteOffEvent off => $"off{off.Channel + 1}:{off.NoteNumber}",
                    ControlChangeEvent control => $"cc{control.Channel + 1}:{control.ControlNumber}",
                    var other => other.EventType.ToString()
                });
            };

            input.StartEventsListening();

            output.NoteOn(2, 60, 100);
            output.NoteOn(2, 60, 80);
            output.NoteOff(2, 60);
            Thread.Sleep(50);
            Assert.Equal(["on2:60:100", "on2:60:80"], received.ToArray());

            output.NoteOff(2, 60);
            output.NoteOn(5, 40, 10);
            output.AllNotesOff();
            Thread.Sleep(100);

            var all = received.ToArray();
            Assert.Equal(["off2:60", "on5:40:10", "off5:40"], all.Skip(2).Take(3));
            Assert.Equal(16, all.Count(x => x.EndsWith(":123")));

            input.StopEventsListening();
        }
    }
}
