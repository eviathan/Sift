using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Sift.Core.Components;
using Sift.Core.Enums;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Nodes;
using Sift.Core.Snapshots;

namespace Sift.Core.Serialisation
{
    // NOTE: Reads and writes sequences as versioned JSON, e.g.
    //
    //   { "format": "sift.sequence", "version": 1, "tempo": 120, "quantisation": "1/16",
    //     "nodes": [ { "id": "…", "name": "Kick", "position": [0, 0, 0], "active": true, "emitOnPlay": true,
    //                  "ports": { "right": [ { "type": "emit" } ],
    //                             "left": [ { "type": "midi", "note": 36, "velocity": 100, "channel": 1, "duration": "sixteenthNote" } ] } } ] }
    //
    // Components are written as their registered type name plus their config record's properties, so new component
    // types need no changes here. Runtime state (messages in flight, looping components) isn't saved
    public class SequenceSerialiser
    {
        public const string FORMAT = "sift.sequence";
        public const int CURRENT_VERSION = 1;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private static readonly JsonSerializerOptions WriteOptions = new(JsonOptions) { WriteIndented = true };

        private readonly ComponentRegistry _registry;
        private readonly ComponentFactory _factory;
        private readonly Dictionary<int, ISequenceMigration> _migrations;

        public SequenceSerialiser(ComponentRegistry registry, ComponentFactory factory, IEnumerable<ISequenceMigration> migrations)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _migrations = migrations.ToDictionary(x => x.FromVersion);
        }

        // NOTE: From a snapshot rather than the live grid so saving is consistent even while playing
        public string Write(SequenceSnapshot snapshot)
        {
            var nodes = new JsonArray();

            foreach (var node in snapshot.Nodes.OrderBy(x => x.Position.Z).ThenBy(x => x.Position.Y).ThenBy(x => x.Position.X))
                nodes.Add(WriteNode(node));

            var root = new JsonObject
            {
                ["format"] = FORMAT,
                ["version"] = CURRENT_VERSION,
                ["tempo"] = snapshot.Tempo,
                ["quantisation"] = $"{snapshot.Quantisation.Numerator}/{snapshot.Quantisation.Denominator}",
                ["nodes"] = nodes,
            };

            // NOTE: Positions on one line, indented JSON otherwise spreads every [x, y, z] over five
            return Regex.Replace(root.ToJsonString(WriteOptions), @"\[\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)\s*\]", "[$1, $2, $3]");
        }

        public LoadedSequence Read(string json)
        {
            JsonObject root;

            try
            {
                root = JsonNode.Parse(json) as JsonObject ?? throw new SequenceFormatException("The file isn't a Sift sequence.");
            }
            catch (JsonException ex)
            {
                throw new SequenceFormatException($"The file isn't valid JSON: {ex.Message}", ex);
            }

            if ((string?)root["format"] != FORMAT)
                throw new SequenceFormatException("The file isn't a Sift sequence.");

            var version = (int?)root["version"] ?? throw new SequenceFormatException("The file has no version.");

            if (version > CURRENT_VERSION)
                throw new SequenceFormatException($"The file was saved by a newer version of Sift (format {version}, this reads up to {CURRENT_VERSION}).");

            for (; version < CURRENT_VERSION; version++)
            {
                if (!_migrations.TryGetValue(version, out var migration))
                    throw new SequenceFormatException($"There's no way to upgrade a format {version} file.");

                migration.Apply(root);
            }

            var tempo = (int?)root["tempo"] ?? 120;
            var quantisation = ReadQuantisation((string?)root["quantisation"]);
            var nodes = new List<Node>();
            var positions = new HashSet<Vector3D>();
            var index = 0;

            foreach (var nodeJson in root["nodes"] as JsonArray ?? [])
            {
                var node = ReadNode(nodeJson as JsonObject ?? throw new SequenceFormatException($"Node {index} isn't an object."), index);

                if (!positions.Add(node.Position))
                    throw new SequenceFormatException($"Node {index} ({node.Name}) is on the same cell as another node, {node.Position}.");

                nodes.Add(node);
                index++;
            }

            return new LoadedSequence(tempo, quantisation, nodes);
        }

        private JsonObject WriteNode(NodeSnapshot node)
        {
            var ports = new JsonObject();

            foreach (var (direction, components) in node.Ports.OrderBy(x => x.Key))
            {
                if (components.Count == 0)
                    continue;

                ports[JsonNamingPolicy.CamelCase.ConvertName(direction.ToString())] = new JsonArray(components.Select(WriteComponent).ToArray<JsonNode?>());
            }

            var json = new JsonObject
            {
                ["id"] = node.Id.ToString(),
                ["name"] = node.Name,
                ["position"] = new JsonArray(node.Position.X, node.Position.Y, node.Position.Z),
                ["active"] = node.IsActive,
                ["emitOnPlay"] = node.EmitOnPlay,
                ["ports"] = ports,
            };

            return json;
        }

        private static JsonObject WriteComponent(ComponentSnapshot component)
        {
            var json = new JsonObject { ["type"] = component.Type };
            var config = JsonSerializer.SerializeToNode(component.Config, component.Config.GetType(), JsonOptions) as JsonObject ?? [];

            foreach (var (name, value) in config.ToList())
                json[name] = value?.DeepClone();

            return json;
        }

        private Node ReadNode(JsonObject json, int index)
        {
            var position = json["position"] as JsonArray;

            if (position is not { Count: 3 })
                throw new SequenceFormatException($"Node {index} needs a position of [x, y, z].");

            var id = Guid.TryParse((string?)json["id"], out var parsed) ? parsed : Guid.NewGuid();
            var node = new Node(new Vector3D((int)position[0]!, (int)position[1]!, (int)position[2]!), id)
            {
                Name = (string?)json["name"],
                IsActive = (bool?)json["active"] ?? true,
                EmitOnPlay = (bool?)json["emitOnPlay"] ?? false,
            };

            foreach (var (portName, componentsJson) in json["ports"] as JsonObject ?? [])
            {
                if (!Enum.TryParse<Direction>(portName, ignoreCase: true, out var direction))
                    throw new SequenceFormatException($"Node {index} ({node.Name}) has an unknown port '{portName}'.");

                foreach (var componentJson in componentsJson as JsonArray ?? [])
                    node.GetPort(direction).Add(ReadComponent(componentJson as JsonObject, node, index));
            }

            return node;
        }

        private IComponent ReadComponent(JsonObject? json, Node node, int index)
        {
            var type = (string?)json?["type"];

            if (json == null || type == null)
                throw new SequenceFormatException($"A component on node {index} ({node.Name}) has no type.");

            if (!_registry.TryGetByName(type, out var registration))
                throw new SequenceFormatException($"Node {index} ({node.Name}) uses a component Sift doesn't know, '{type}'.");

            var configJson = (JsonObject)json.DeepClone();
            configJson.Remove("type");

            try
            {
                var config = (IComponentConfig?)configJson.Deserialize(registration.ConfigType, JsonOptions)
                    ?? throw new SequenceFormatException($"A '{type}' component on node {index} ({node.Name}) has no settings.");

                // NOTE: Creating it validates the config
                return _factory.Create(config);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                throw new SequenceFormatException($"A '{type}' component on node {index} ({node.Name}) has invalid settings: {ex.Message}", ex);
            }
        }

        private static Quantisation ReadQuantisation(string? value)
        {
            var parts = (value ?? "1/16").Split('/');

            if (parts.Length != 2 || !int.TryParse(parts[0], out var numerator) || !int.TryParse(parts[1], out var denominator))
                throw new SequenceFormatException($"'{value}' isn't a quantisation, it should look like 1/16.");

            try
            {
                return new Quantisation(numerator, denominator);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new SequenceFormatException($"'{value}' is out of range for a quantisation.", ex);
            }
        }
    }
}
