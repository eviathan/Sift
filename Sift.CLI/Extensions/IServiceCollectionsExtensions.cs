using Microsoft.Extensions.DependencyInjection;
using Sift.CLI.Services;
using Sift.Core.Extensions;
using Sift.Services;

namespace Sift.CLI.Extensions
{
    public static class IServiceCollectionsExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSiftCore();

            serviceCollection.AddSingleton<App>();
            serviceCollection.AddSingleton<IKeyEventService<ConsoleKey>, ConsoleKeyEventService>();

            return serviceCollection;
        }
    }
}
