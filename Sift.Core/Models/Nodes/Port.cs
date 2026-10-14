using Sift.Core.Interfaces;

namespace Sift.Core.Models.Nodes
{
    public class Port
    {
        public List<IComponent> Components { get; set; } = [];
    }
}