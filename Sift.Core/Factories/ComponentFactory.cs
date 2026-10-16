using Microsoft.Extensions.DependencyInjection;
using Sift.Core.Components;
using Sift.Core.Interfaces;

namespace Sift.Core.Factories
{
    public class ComponentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ComponentRegistry _registry;

        public ComponentFactory(IServiceProvider serviceProvider, ComponentRegistry registry)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        // NOTE: Each call creates a new component instance so runtime state isn't shared between ports
        public IComponent Create(IComponentConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (!_registry.TryGetByConfigType(config.GetType(), out var registration))
                throw new InvalidOperationException($"No component is registered for config {config.GetType().Name}.");

            var component = (IComponent)ActivatorUtilities.CreateInstance(_serviceProvider, registration.ComponentType);
            component.Configure(config);

            return component;
        }
    }
}
