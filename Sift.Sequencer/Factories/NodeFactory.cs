using Microsoft.Extensions.DependencyInjection;
using Sift.Sequencer.Grid;
using Sift.Sequencer.Nodes;

namespace Sift.Sequencer.Factories
{
    public class NodeFactory
    {
        private readonly  IServiceProvider _serviceProvider;

        public NodeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        async Task<Node> CreateNodeAsync(Vector3D position)
        {
            var node = ActivatorUtilities.CreateInstance<Node>(_serviceProvider);

            return await Task.FromResult(node);
        }
    }
}