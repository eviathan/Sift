using Sift.Core.Models.Nodes;
using Sift.Core.Nodes;

namespace Sift.Core.Factories
{
    public class ComponentEventArgsFactory
    {
        private readonly Sequence _sequence;

        public ComponentEventArgsFactory(Sequence sequence)
        {
            _sequence = sequence;
        }

        public ComponentEventArgs Create(Node node)
        {
            var args = Activator.CreateInstance<ComponentEventArgs>();
            args.Node = node;

            return args;
        }
    }
}