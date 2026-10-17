using System.Collections.Concurrent;
using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;

namespace Sift.Tests.Unit.Support
{
    // NOTE: Shared with the test components through DI, one per fixture, instead of statics
    public sealed class TestCounters
    {
        private int _running;

        public int Running => Volatile.Read(ref _running);

        public ConcurrentQueue<(long Tick, double Milliseconds)> Stamps { get; } = new();

        public System.Diagnostics.Stopwatch Stopwatch { get; } = new();

        public void Started() => Interlocked.Increment(ref _running);

        public void Finished() => Interlocked.Decrement(ref _running);
    }

    // NOTE: Emits out of its port every IntervalMs of wall-clock time, from the thread pool, until cancelled
    public sealed record PulseConfig(int IntervalMs) : IComponentConfig;

    [ComponentType("test-pulse")]
    public sealed class PulseComponent(TestCounters counters) : Component<PulseConfig>
    {
        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            counters.Started();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    context.Sequencer.Emit(context.Node.Id, context.Port, new Message { Payload = "pulse" });
                    await Task.Delay(Config.IntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                counters.Finished();
            }
        }
    }

    // NOTE: Emits out of its port every Interval ticks of musical time, or once when Once is set
    public sealed record TickPulseConfig(long Interval, bool Once = false) : IComponentConfig;

    [ComponentType("test-tick-pulse")]
    public sealed class TickPulseComponent(TestCounters counters) : Component<TickPulseConfig>
    {
        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            counters.Started();

            try
            {
                var time = context.DispatchTime;

                do
                {
                    time = await context.Sequencer.WaitUntilAsync(time + Config.Interval, cancellationToken);
                    context.Sequencer.Emit(context.Node.Id, context.Port, new Message { Payload = "tick" }, time);
                }
                while (!Config.Once && !cancellationToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                counters.Finished();
            }
        }
    }

    public sealed record FaultConfig : IComponentConfig;

    [ComponentType("test-fault")]
    public sealed class FaultComponent : Component<FaultConfig>
    {
        public override async Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            throw new InvalidOperationException("boom");
        }
    }

    // NOTE: Records the tick and wall-clock time it was invoked, for measuring the real clock
    public sealed record StampConfig : IComponentConfig;

    [ComponentType("test-stamp")]
    public sealed class StampComponent(TestCounters counters) : Component<StampConfig>
    {
        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            counters.Stamps.Enqueue((context.DispatchTime.Ticks, counters.Stopwatch.Elapsed.TotalMilliseconds));
            return Task.CompletedTask;
        }
    }
}
