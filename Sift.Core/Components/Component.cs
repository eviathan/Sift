using Sift.Core.Enums;
using Sift.Core.Interfaces;
using Sift.Core.Models;
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
            var typedConfig = config as TConfig
                ?? throw new ArgumentException($"{GetType().Name} expects {typeof(TConfig).Name} but got {config?.GetType().Name ?? "null"}.", nameof(config));

            typedConfig.Validate();
            Config = typedConfig;
        }

        public abstract Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken);

        // NOTE: Sends the message on: out of `to` when set, otherwise straight on. On the dispatch tick by default so it stays exact
        protected static void Forward(ComponentContext context, Direction? to, Message? message = null, Time? at = null) =>
            context.Sequencer.Emit(context.Node.Id, to ?? context.StraightOn, message ?? context.Message, at ?? context.DispatchTime);
    }
}
