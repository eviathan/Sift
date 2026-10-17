using Sift.Core.Components;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core.Snapshots
{
    internal static class SnapshotFactory
    {
        public static NodeInfo CreateInfo(Node node) =>
            new(node.Id, node.Name, node.Position, node.IsActive, node.EmitOnPlay);

        public static NodeSnapshot Create(Node node, ComponentRegistry registry) =>
            new(node.Id,
                node.Name,
                node.Position,
                node.IsActive,
                node.EmitOnPlay,
                node.Bus.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<ComponentSnapshot>)x.Value.Components.Select(c => Create(c, registry)).ToList()));

        public static ComponentSnapshot Create(IComponent component, ComponentRegistry registry) =>
            registry.TryGetByConfigType(component.Config.GetType(), out var registration)
                ? new(component.Id, registration.Name, registration.Category, component.Config)
                : new(component.Id, component.GetType().Name, Enums.ComponentCategory.Debug, component.Config);

        public static InFlightSnapshot Create(InFlightMessage message) =>
            new(message.Id,
                message.SourceNodeId,
                message.Origin,
                message.Direction,
                message.EmitTime,
                message.TicksPerCell,
                message.Message.Payload,
                message.TargetNodeId,
                message.EndPosition,
                message.ArrivalTime);
    }
}
