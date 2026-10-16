using Sift.Core.Models.Nodes;

namespace Sift.Core.Interfaces
{
    public interface IComponent
    {
        Guid Id { get; }

        IComponentConfig Config { get; }

        void Configure(IComponentConfig config);

        Task InvokeAsync(ComponentContext context, CancellationToken cancellationToken);
    }
}
