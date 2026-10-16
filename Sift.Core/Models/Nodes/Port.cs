using Sift.Core.Interfaces;

namespace Sift.Core.Models.Nodes
{
    public class Port
    {
        public List<IComponent> Components { get; } = [];

        public Port Add(IComponent component)
        {
            Components.Add(component ?? throw new ArgumentNullException(nameof(component)));
            return this;
        }
    }
}
