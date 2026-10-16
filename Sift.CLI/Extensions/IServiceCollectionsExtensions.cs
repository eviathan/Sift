using Microsoft.Extensions.DependencyInjection;
using Sift.CLI.Services;
using Sift.Core;
using Sift.Core.Factories;
using Sift.Core.Models;
using Sift.Core.Models.Nodes;
using Sift.Services;

namespace Sift.CLI.Extensions
{
    public static class IServiceCollectionsExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<App>();        
            serviceCollection.AddSingleton<EventScheduler>();
            serviceCollection.AddSingleton<SpatialPropagator>();
            serviceCollection.AddSingleton<NodeFactory>();
            serviceCollection.AddSingleton<ComponentEventArgsFactory>();
            
            serviceCollection.AddScoped<NodeBus>();
            serviceCollection.AddScoped<Port>();
            serviceCollection.AddScoped<Sequence>();
            serviceCollection.AddScoped<Sequencer>();
            serviceCollection.AddScoped<ComponentEventArgs>();

            serviceCollection.AddScoped<IKeyEventService<ConsoleKey>, ConsoleKeyEventService>();

            return serviceCollection;
        }
    }
}