using Microsoft.Extensions.Logging;
using Sift.Core.Interfaces;
using Sift.Core.Models.Nodes;

namespace Sift.Core.Components
{
    public sealed record LogConfig : IComponentConfig;

    [ComponentType("log")]
    public class LogComponent : Component<LogConfig>
    {
        private readonly ILogger<LogComponent> _logger;

        public LogComponent(ILogger<LogComponent> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken)
        {
            var trigger = context.IsPlay ? "play" : "message";
            var name = context.Node.Name ?? context.Node.Id.ToString();

            _logger.LogInformation($"[Tick {context.DispatchTime.Ticks}] {name} {context.Node.Position} received '{context.Message.Payload}' on {context.Port} ({trigger})");

            return Task.CompletedTask;
        }
    }
}
