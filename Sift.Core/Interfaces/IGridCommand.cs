using Sift.Core.Models;
using Sift.Core.Nodes;

namespace Sift.Core.Interfaces
{
    // NOTE: Every change to the grid goes through a command so it's applied on the clock thread between ticks
    public interface IGridCommand
    {
        // NOTE: True when nodes are added, removed or moved, which means in-flight messages need re-resolving
        bool AffectsSpace { get; }

        // NOTE: Throws if it can't be applied. Returns the command that undoes it
        IGridCommand Apply(Grid<Node> grid);
    }
}
