using System.Reflection;
using Sift.Core.Interfaces;

namespace Sift.Core.Components
{
    public sealed record ComponentRegistration(string Name, Type ComponentType, Type ConfigType);

    public class ComponentRegistry
    {
        private readonly Dictionary<string, ComponentRegistration> _byName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Type, ComponentRegistration> _byConfigType = [];

        public IEnumerable<ComponentRegistration> Registrations => _byName.Values;

        public ComponentRegistry()
        {
            RegisterAssembly(typeof(ComponentRegistry).Assembly);
        }

        public void RegisterAssembly(Assembly assembly)
        {
            var componentTypes = assembly.GetTypes()
                .Where(x => !x.IsAbstract && typeof(IComponent).IsAssignableFrom(x) && x.GetCustomAttribute<ComponentTypeAttribute>() != null);

            foreach (var componentType in componentTypes)
                Register(componentType);
        }

        public void Register<TComponent>()
            where TComponent : IComponent =>
            Register(typeof(TComponent));

        public void Register(Type componentType)
        {
            var attribute = componentType.GetCustomAttribute<ComponentTypeAttribute>()
                ?? throw new ArgumentException($"{componentType.Name} is missing a [ComponentType] attribute.");

            var configType = GetConfigType(componentType)
                ?? throw new ArgumentException($"{componentType.Name} must derive from Component<TConfig>.");

            var registration = new ComponentRegistration(attribute.Name, componentType, configType);

            if (!_byName.TryAdd(attribute.Name, registration))
                throw new InvalidOperationException($"Component type '{attribute.Name}' is already registered.");

            if (!_byConfigType.TryAdd(configType, registration))
            {
                _byName.Remove(attribute.Name);
                throw new InvalidOperationException($"Config {configType.Name} is already used by component '{_byConfigType[configType].Name}'.");
            }
        }

        public bool TryGetByName(string name, out ComponentRegistration registration) =>
            _byName.TryGetValue(name, out registration!);

        public bool TryGetByConfigType(Type configType, out ComponentRegistration registration) =>
            _byConfigType.TryGetValue(configType, out registration!);

        private static Type? GetConfigType(Type componentType)
        {
            for (var type = componentType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Component<>))
                    return type.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
