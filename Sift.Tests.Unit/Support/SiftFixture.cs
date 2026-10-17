using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sift.Core;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Extensions;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Nodes;
using Sift.Core.Serialisation;

namespace Sift.Tests.Unit.Support
{
    // NOTE: A complete Sift.Core for one test: its own services, log capture, fake MIDI output and the test components.
    // Nothing is static, so test classes can run in parallel
    public sealed class SiftFixture : IDisposable
    {
        private readonly ServiceProvider _services;

        public LogCapture Log { get; } = new();
        public FakeMidiOutput Midi { get; }
        public TestCounters Counters { get; } = new();

        public Sequence Sequence => _services.GetRequiredService<Sequence>();
        public SequencerRuntime Runtime => _services.GetRequiredService<SequencerRuntime>();
        public ComponentFactory Components => _services.GetRequiredService<ComponentFactory>();
        public ComponentRegistry Registry => _services.GetRequiredService<ComponentRegistry>();
        public RuntimeEventHub Events => _services.GetRequiredService<RuntimeEventHub>();
        public SequenceSerialiser Serialiser => _services.GetRequiredService<SequenceSerialiser>();

        public SiftFixture(params ISequenceMigration[] migrations)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(Log).SetMinimumLevel(LogLevel.Information));
            services.AddSiftCore();
            services.AddSingleton(Counters);

            Midi = new FakeMidiOutput(() => _services!.GetRequiredService<SequencerRuntime>().Now.Ticks);
            services.AddSingleton<IMidiOutput>(Midi);

            foreach (var migration in migrations)
                services.AddSingleton(migration);

            _services = services.BuildServiceProvider();

            Registry.Register<PulseComponent>();
            Registry.Register<TickPulseComponent>();
            Registry.Register<FaultComponent>();
            Registry.Register<StampComponent>();
        }

        // NOTE: The Sequencer starts a real clock thread and the real MIDI output isn't used here, so it's built on demand
        public Sequencer CreateSequencer() =>
            _services.GetRequiredService<Sequencer>();

        public Node Add(Node node)
        {
            Sequence.Grid.AddNode(node);
            return node;
        }

        // NOTE: A node that starts on play with the given components on one port
        public Node Source(Vector3D position, Direction port, params IComponentConfig[] configs) =>
            Add(Build(new Node(position) { Name = "Source", EmitOnPlay = true }, port, configs));

        public Node Emitter(Vector3D position, Direction port) =>
            Source(position, port, new EmitConfig());

        // NOTE: A node that logs what arrives on one port, read back with ArrivalsAt
        public Node Listener(Vector3D position, Direction port, string name) =>
            Add(Build(new Node(position) { Name = name }, port, new LogConfig()));

        public Node Node(Vector3D position, string name, Direction port, params IComponentConfig[] configs) =>
            Add(Build(new Node(position) { Name = name }, port, configs));

        public Node Build(Node node, Direction port, params IComponentConfig[] configs)
        {
            foreach (var config in configs)
                node.GetPort(port).Add(Components.Create(config));

            return node;
        }

        // NOTE: The ticks a Listener logged messages arriving on, in order
        public IReadOnlyList<long> ArrivalsAt(string name) =>
            Log.Lines.Where(x => x.Contains("received") && x.Contains($"] {name} "))
                .Select(x => long.Parse(x[6..x.IndexOf(']')]))
                .ToList();

        public IReadOnlyList<string> Arrivals =>
            Log.Lines.Where(x => x.Contains("received")).ToList();

        // NOTE: Drives the clock at roughly a tick a millisecond (faster than 120 BPM) so components resuming on the
        // thread pool keep up, as they do with the real clock
        public void Play(long until, long step = 1)
        {
            for (var tick = Runtime.Now.Ticks; tick <= until; tick += step)
            {
                Runtime.AdvanceTo(new Time(tick));
                Thread.Sleep(1);
            }

            Thread.Sleep(20);
            Runtime.AdvanceTo(new Time(until));
        }

        public void Dispose()
        {
            if (Runtime.IsRunning)
                Runtime.Stop();

            _services.Dispose();
        }
    }

    public sealed class LogCapture : ILoggerProvider, ILogger
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines
        {
            get { lock (_lines) return _lines.ToList(); }
        }

        public bool Contains(string text) =>
            Lines.Any(x => x.Contains(text));

        public void Clear()
        {
            lock (_lines) _lines.Clear();
        }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_lines) _lines.Add(formatter(state, exception));
        }

        public void Dispose()
        {
        }
    }

    public sealed class FakeMidiOutput : IMidiOutput
    {
        private readonly Func<long> _now;
        private readonly ConcurrentQueue<string> _events = new();

        public FakeMidiOutput(Func<long> now) =>
            _now = now;

        public IReadOnlyList<string> Events => _events.ToList();

        public bool IsAvailable => true;

        public void NoteOn(int channel, int note, int velocity) => _events.Enqueue($"on ch{channel} n{note} v{velocity} @{_now()}");

        public void NoteOff(int channel, int note) => _events.Enqueue($"off ch{channel} n{note} @{_now()}");

        public void AllNotesOff() => _events.Enqueue("all-off");

        public void Clear() => _events.Clear();
    }

    public static class EventReading
    {
        public static List<RuntimeEvent> ReadAll(this RuntimeEventSubscription subscription)
        {
            var events = new List<RuntimeEvent>();

            while (subscription.Reader.TryRead(out var runtimeEvent))
                events.Add(runtimeEvent);

            return events;
        }
    }
}
