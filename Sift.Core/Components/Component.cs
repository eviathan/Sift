using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public abstract class Component<TConfig> : IComponent
        where TConfig : class, IComponentConfig
    {
        public Guid Id { get; } = Guid.NewGuid();

        public TConfig Config { get; private set; } = null!;

        IComponentConfig IComponent.Config => Config;

        public void Configure(IComponentConfig config)
        {
            Config = config as TConfig
                ?? throw new ArgumentException($"{GetType().Name} expects {typeof(TConfig).Name} but got {config?.GetType().Name ?? "null"}.", nameof(config));
        }

        public abstract Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken);
    }
}
