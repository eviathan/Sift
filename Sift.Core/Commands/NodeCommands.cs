using Sift.Core.Grid;
using Sift.Core.Interfaces;
using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core.Commands
{
    public sealed record AddNode(Node Node) : IGridCommand
    {
        public bool AffectsSpace => true;

        public IGridCommand Apply(Grid<Node> grid)
        {
            grid.AddNode(Node);
            return new RemoveNode(Node.Id);
        }
    }

    public sealed record RemoveNode(Guid NodeId) : IGridCommand
    {
        public bool AffectsSpace => true;

        // NOTE: The inverse re-adds the same instance so its components (and their state) come back with it
        public IGridCommand Apply(Grid<Node> grid)
        {
            if (!grid.Remove(NodeId, out var node))
                throw new KeyNotFoundException($"No node found with id {NodeId}.");

            return new AddNode(node);
        }
    }

    public sealed record MoveNode(Guid NodeId, Vector3D To) : IGridCommand
    {
        public bool AffectsSpace => true;

        public IGridCommand Apply(Grid<Node> grid)
        {
            var from = grid.GetNode(NodeId).Position;
            grid.Move(NodeId, To);

            return new MoveNode(NodeId, from);
        }
    }

    // NOTE: Null values are left unchanged
    public sealed record UpdateNode(Guid NodeId, string? Name = null, bool? IsActive = null, bool? EmitOnPlay = null) : IGridCommand
    {
        public bool AffectsSpace => false;

        public IGridCommand Apply(Grid<Node> grid)
        {
            var node = grid.GetNode(NodeId);

            var inverse = new UpdateNode(
                NodeId,
                Name != null ? node.Name ?? string.Empty : null,
                IsActive.HasValue ? node.IsActive : null,
                EmitOnPlay.HasValue ? node.EmitOnPlay : null);

            if (Name != null)
                node.Name = Name;

            if (IsActive.HasValue)
                node.IsActive = IsActive.Value;

            if (EmitOnPlay.HasValue)
                node.EmitOnPlay = EmitOnPlay.Value;

            return inverse;
        }
    }
}
