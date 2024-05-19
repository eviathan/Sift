using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.NodeEvents
{
    public class LogNodeEvent : INodeEvent
    {
        public void Invoke(Node node)
        {
            Console.WriteLine($"{node.Position.X}_{node.Position.Y}_{node.Position.Z}");
        }
    }
}