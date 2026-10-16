using Sift.Core.Enums;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Models
{
    public class NodeBus : Dictionary<Direction, Port>
    {
        public NodeBus()
        {
            foreach (Direction direction in Enum.GetValues<Direction>())
                Add(direction, new Port());
        }
    }
}
