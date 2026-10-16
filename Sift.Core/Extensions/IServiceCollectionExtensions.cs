using Microsoft.Extensions.DependencyInjection;
using Sift.Core.Components;
using Sift.Core.Factories;

namespace Sift.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddSiftCore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Sequence>();
            serviceCollection.AddSingleton<Sequencer>();
            serviceCollection.AddSingleton<SequencerRuntime>();
            serviceCollection.AddSingleton<EventScheduler>();
            serviceCollection.AddSingleton<SpatialPropagator>();
            serviceCollection.AddSingleton<ComponentRegistry>();
            serviceCollection.AddSingleton<ComponentFactory>();

            return serviceCollection;
        }
    }
}
