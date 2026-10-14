using Microsoft.Extensions.DependencyInjection;
using Sift.Core.Grid;
using Sift.Core.Nodes;

namespace Sift.Core.Factories
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