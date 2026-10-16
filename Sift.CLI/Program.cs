using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sift.CLI.Extensions;
using Microsoft.Extensions.Logging;

namespace Sift.CLI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
                
            var services = new ServiceCollection();

            services.AddLogging(logging =>
            {
                logging.AddConsole();
            });
            
            services.AddSingleton<IConfiguration>(configuration);
            services.AddServices();
            
            var provider = services.BuildServiceProvider();

            var app = provider.GetRequiredService<App>();

            await app.Init();

            while (app.IsRunning)
            {
                app.Update();
            }
        }
    }
};