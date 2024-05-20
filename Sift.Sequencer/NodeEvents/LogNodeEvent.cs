using Sift.Sequencer.Interfaces;
using Sift.Sequencer.Models.Nodes;

namespace Sift.Sequencer.NodeEvents
{
    public class LogNodeEvent : INodeEvent
    {
        public void Invoke(NodeEventArgs e)
        {
            Console.WriteLine($"{e.Node.Position.X}_{e.Node.Position.Y}_{e.Node.Position.Z}");
        }
    }
}