using System.Text.Json.Nodes;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Events;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Music;
using Sift.Core.Nodes;
using Sift.Core.Serialisation;
using Sift.Core.Snapshots;
using Sift.Tests.Unit.Support;

namespace Sift.Tests.Unit
{
    public class SerialisationTests
    {
        [Fact]
        public void A_sequence_round_trips_through_json()
        {
            using var sift = new SiftFixture();
            sift.Sequence.Tempo = 97;
            sift.Sequence.Quantisation = new Quantisation(1, 8);

            var start = sift.Source(new(0, 0, 0), Direction.Right, new EmitConfig());
            sift.Build(start, Direction.Up,
                new MidiConfig(Note: 72, Velocity: 90, Channel: 3, Duration: NoteDuration.EighthNote),
                new RedirectConfig(Direction.Forwards));
            sift.Add(sift.Build(new Node(new(4, -2, 1)) { Name = "B", IsActive = false }, Direction.Backwards, new LogConfig()));

            var json = sift.Serialiser.Write(sift.Runtime.GetSnapshotAsync().Result);

            using var loaded = new SiftFixture();
            using var events = loaded.Events.Subscribe();
            loaded.Runtime.Replace(loaded.Serialiser.Read(json));

            Assert.Equal(Describe(sift.Runtime.GetSnapshotAsync().Result), Describe(loaded.Runtime.GetSnapshotAsync().Result));
            Assert.Equal(97, loaded.Sequence.Tempo);
            Assert.Equal(8, loaded.Sequence.Quantisation.Denominator);
            Assert.Contains(events.ReadAll(), x => x is SequenceReplaced);

            Assert.Contains("\"emitOnPlay\": true", json);
            Assert.Contains("\"duration\": \"eighthNote\"", json);
            Assert.Contains("\"to\": \"forwards\"", json);
            Assert.Contains("\"position\": [4, -2, 1]", json);
        }

        [Fact]
        public void Every_built_in_component_config_round_trips()
        {
            using var sift = new SiftFixture();
            IComponentConfig[] configs =
            [
                new EmitConfig(), new LogConfig(), new RedirectConfig(Direction.Left), new MidiConfig(61, 70, 2, NoteDuration.HalfNote),
                new ClockConfig(NoteDuration.EighthNote, 5), new DivideConfig(3, Direction.Down), new ChanceConfig(25), new ScatterConfig(true),
                new DelayConfig(NoteDuration.HalfNote, Direction.Left), new SplitConfig(false, true, false, true, true, false), new RotateConfig(false),
                new GateConfig(false, Direction.Up), new ToggleConfig(false), new MoveConfig(Direction.Backwards, 3), new TransposeConfig(-7),
                new NotesConfig("A3 C4", NoteOrder.UpDown, 90, 2, NoteDuration.EighthNote), new ChordConfig(57, ChordQuality.Minor7, 2, 80, 3, NoteDuration.HalfNote),
            ];

            var node = sift.Add(sift.Build(new Node(new(0, 0, 0)), Direction.Up, configs));
            var json = sift.Serialiser.Write(sift.Runtime.GetSnapshotAsync().Result);
            var loaded = sift.Serialiser.Read(json).Nodes.Single().GetPort(Direction.Up).Components.Select(x => x.Config);

            Assert.Equal(configs, loaded);

            // NOTE: Every registered type outside the tests' own is covered
            var covered = configs.Select(x => sift.Registry.Registrations.Single(r => r.ConfigType == x.GetType()).Name).ToHashSet();
            Assert.All(sift.Registry.Registrations.Where(x => !x.Name.StartsWith("test-")), x => Assert.Contains(x.Name, covered));
        }

        [Theory]
        [InlineData("{ nope", "isn't valid JSON")]
        [InlineData("{\"format\":\"other\"}", "isn't a Sift sequence")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":99}", "newer version")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"nodes\":[{\"name\":\"X\",\"position\":[0,0,0],\"ports\":{\"up\":[{\"type\":\"warp\"}]}}]}", "doesn't know, 'warp'")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"nodes\":[{\"name\":\"X\",\"position\":[0,0,0],\"ports\":{\"up\":[{\"type\":\"midi\",\"note\":500}]}}]}", "invalid settings")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"nodes\":[{\"position\":[1,1,0]},{\"position\":[1,1,0]}]}", "same cell")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"nodes\":[{\"position\":[0,0,0],\"ports\":{\"sideways\":[]}}]}", "unknown port")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"nodes\":[{\"position\":[0,0]}]}", "position of [x, y, z]")]
        [InlineData("{\"format\":\"sift.sequence\",\"version\":1,\"quantisation\":\"one\"}", "isn't a quantisation")]
        public void Bad_files_are_refused_with_a_clear_message(string json, string message)
        {
            using var sift = new SiftFixture();

            var error = Assert.Throws<SequenceFormatException>(() => sift.Serialiser.Read(json));
            Assert.Contains(message, error.Message);
        }

        [Fact]
        public void Older_files_are_migrated_and_unknown_versions_refused()
        {
            using var migrating = new SiftFixture(new RenameLabelToName());
            var loaded = migrating.Serialiser.Read("{\"format\":\"sift.sequence\",\"version\":0,\"nodes\":[{\"label\":\"Old\",\"position\":[0,0,0]}]}");
            Assert.Equal("Old", loaded.Nodes.Single().Name);

            using var plain = new SiftFixture();
            var error = Assert.Throws<SequenceFormatException>(() => plain.Serialiser.Read("{\"format\":\"sift.sequence\",\"version\":0}"));
            Assert.Contains("upgrade", error.Message);
        }

        [Fact]
        public void Replacing_the_sequence_clears_undo_and_is_refused_while_playing()
        {
            using var sift = new SiftFixture();
            sift.Runtime.Submit(new Sift.Core.Commands.AddNode(new Node(new(0, 0, 0)))).Wait();

            sift.Runtime.Replace(LoadedSequence.Empty());

            Assert.Equal(0, sift.Sequence.Grid.Count);
            Assert.False(sift.Runtime.Undo().Result);

            sift.Runtime.Start();
            Assert.Throws<InvalidOperationException>(() => sift.Runtime.Replace(LoadedSequence.Empty()));
        }

        [Fact]
        public void The_example_file_loads()
        {
            using var sift = new SiftFixture();
            var path = Path.Combine(RepositoryRoot(), "Sift.App", "Examples", "Demo.sift");

            var loaded = sift.Serialiser.Read(File.ReadAllText(path));

            Assert.NotEmpty(loaded.Nodes);
        }

        private static string Describe(SequenceSnapshot snapshot) =>
            string.Join("\n", snapshot.Nodes.OrderBy(x => x.Id).Select(node =>
                $"{node.Id}|{node.Name}|{node.Position}|{node.IsActive}|{node.EmitOnPlay}|" +
                string.Join(";", node.Ports.OrderBy(x => x.Key).Select(port => port.Key + "=" + string.Join(",", port.Value.Select(x => x.Type + ":" + x.Config))))));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Sift.sln")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException("Couldn't find Sift.sln above the test output");
        }

        private sealed class RenameLabelToName : ISequenceMigration
        {
            public int FromVersion => 0;

            public void Apply(JsonObject root)
            {
                foreach (var node in root["nodes"] as JsonArray ?? [])
                {
                    var json = (JsonObject)node!;

                    if (json["label"] is { } label)
                    {
                        json["name"] = label.DeepClone();
                        json.Remove("label");
                    }
                }
            }
        }
    }
}
