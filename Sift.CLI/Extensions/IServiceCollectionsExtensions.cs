using Microsoft.Extensions.DependencyInjection;
using Sift.CLI.Services;
using Sift.Sequencer;
using Sift.Sequencer.Factories;
using Sift.Sequencer.Models;
using Sift.Sequencer.Models.Nodes;
using Sift.Services;

namespace Sift.CLI.Extensions
{
    public static class IServiceCollectionsExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<App>();        
            
            serviceCollection.AddScoped<NodeBus>();
            serviceCollection.AddScoped<NodeFactory>();
            serviceCollection.AddScoped<Port>();
            serviceCollection.AddScoped<Sequence>();
            serviceCollection.AddScoped<SiftSequencer>();

            serviceCollection.AddScoped<IKeyEventService<ConsoleKey>, ConsoleKeyEventService>();

            return serviceCollection;
        }
    }
}