using Microsoft.Extensions.DependencyInjection;
using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Models
{
    public class NodeBus : Dictionary<Direction, Port>
    {
        private readonly IServiceProvider _serviceProvider;

        public NodeBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            foreach (Direction direction in Enum.GetValues<Direction>())
                Add(direction, ActivatorUtilities.CreateInstance<Port>(serviceProvider));
        }

        public void AddComponent<TComponent>(Direction direction)
            where TComponent : class, IComponent, new()
        {
            var component = ActivatorUtilities.CreateInstance<TComponent>(_serviceProvider);
            
            this[direction].Components.Add(component);
        }
    }
}