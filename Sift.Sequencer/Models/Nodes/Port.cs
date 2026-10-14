using Sift.Sequencer.Interfaces;

namespace Sift.Sequencer.Models.Nodes
{
    public class Port
    {
        public List<IComponent> Components { get; set; } = [];
    }
}