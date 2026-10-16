using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core.Commands
{
    // NOTE: Create the component with the ComponentFactory before submitting. Index null appends
    public sealed record AddComponent(Guid NodeId, Direction Port, IComponent Component, int? Index = null) : IGridCommand
    {
        public bool AffectsSpace => false;

        public IGridCommand Apply(Grid<Node> grid)
        {
            var components = grid.GetNode(NodeId).GetPort(Port).Components;
            components.Insert(Math.Clamp(Index ?? components.Count, 0, components.Count), Component);

            return new RemoveComponent(NodeId, Port, Component.Id);
        }
    }

    // TODO: Phase 3, cancel a removed component that is still looping
    public sealed record RemoveComponent(Guid NodeId, Direction Port, Guid ComponentId) : IGridCommand
    {
        public bool AffectsSpace => false;

        public IGridCommand Apply(Grid<Node> grid)
        {
            var components = grid.GetNode(NodeId).GetPort(Port).Components;
            var index = components.FindIndex(x => x.Id == ComponentId);

            if (index < 0)
                throw new KeyNotFoundException($"No component {ComponentId} on Node {NodeId} Port {Port}.");

            var component = components[index];
            components.RemoveAt(index);

            return new AddComponent(NodeId, Port, component, index);
        }
    }

    public sealed record UpdateComponentConfig(Guid NodeId, Direction Port, Guid ComponentId, IComponentConfig Config) : IGridCommand
    {
        public bool AffectsSpace => false;

        public IGridCommand Apply(Grid<Node> grid)
        {
            var component = grid.GetNode(NodeId).GetPort(Port).Components.FirstOrDefault(x => x.Id == ComponentId)
                ?? throw new KeyNotFoundException($"No component {ComponentId} on Node {NodeId} Port {Port}.");

            var previous = component.Config;
            component.Configure(Config);

            return new UpdateComponentConfig(NodeId, Port, ComponentId, previous);
        }
    }
}
