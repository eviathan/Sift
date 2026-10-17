using System.Diagnostics;
using Sift.Core;
using Sift.Core.Commands;
using Sift.Core.Enums;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    // NOTE: Components are fire and forget by design: looping ones run on the thread pool alongside the clock while the
    // user edits from another thread
    public class ConcurrencyTests
    {
        [Fact]
        public void Looping_components_and_concurrent_edits_run_without_errors_and_stop_promptly()
        {
            using var sift = new SiftFixture();
            var pulser = sift.Source(new(0, 0, 0), Direction.Right, new PulseConfig(2));
            sift.Build(pulser, Direction.Up, new PulseConfig(3));
            sift.Listener(new(2, 0, 0), Direction.Left, "Right");
            sift.Listener(new(0, 2, 0), Direction.Down, "Up");
            var mover = sift.Listener(new(5, 5, 5), Direction.Left, "Mover");

            sift.Runtime.Start();

            using var editing = new CancellationTokenSource();
            var editor = Task.Run(() =>
            {
                var step = 0;

                while (!editing.IsCancellationRequested)
                {
                    _ = sift.Runtime.Submit(new MoveNode(mover.Id, new(1, (step++ % 2) * 7 + 10, 0)));
                    Thread.Sleep(1);
                }
            });

            var started = Stopwatch.StartNew();
            long tick = 0;

            while (started.Elapsed < TimeSpan.FromSeconds(1))
            {
                tick += 10;
                sift.Runtime.AdvanceTo(new Time(tick));
                Thread.Sleep(1);
            }

            editing.Cancel();
            editor.Wait();

            Assert.NotEmpty(sift.ArrivalsAt("Right"));
            Assert.NotEmpty(sift.ArrivalsAt("Up"));
            Assert.False(sift.Log.Contains("faulted") || sift.Log.Contains("threw"));

            var stopping = Stopwatch.StartNew();
            sift.Runtime.Stop();

            Assert.True(stopping.ElapsedMilliseconds < 400, $"stop took {stopping.ElapsedMilliseconds}ms");
            Assert.False(sift.Log.Contains("did not stop"));
            Assert.Equal(0, sift.Counters.Running);
        }

        [Fact]
        public void A_faulting_component_is_logged_and_playback_continues()
        {
            using var sift = new SiftFixture();
            var source = sift.Emitter(new(0, 0, 0), Direction.Right);
            sift.Build(source, Direction.Up, new FaultConfig());
            sift.Listener(new(1, 0, 0), Direction.Left, "After");

            sift.Runtime.Start();
            sift.Runtime.AdvanceTo(new Time(200));
            Thread.Sleep(100);

            Assert.True(sift.Log.Contains("faulted"));
            Assert.NotEmpty(sift.ArrivalsAt("After"));
        }
    }
}
