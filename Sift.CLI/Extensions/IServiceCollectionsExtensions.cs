using Microsoft.Extensions.DependencyInjection;
using Sift.Sequencer.Factories;
using Sift.Sequencer.Models;
using Sift.Sequencer.Models.Nodes;

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

            return serviceCollection;
        }
    }
}