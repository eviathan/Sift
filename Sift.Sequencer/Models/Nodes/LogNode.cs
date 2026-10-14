using Sift.Sequencer.Components;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Models.Nodes
{
    public class LogNode : Node
    {
        public LogNode(NodeBus bus) : base(bus)
        {
            Bus.AddComponent<LogComponent>(Enums.Direction.Up);
            Bus.AddComponent<LogComponent>(Enums.Direction.Right);
            Bus.AddComponent<LogComponent>(Enums.Direction.Down);
            Bus.AddComponent<LogComponent>(Enums.Direction.Left);
        }
    }
}
