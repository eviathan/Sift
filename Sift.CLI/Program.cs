using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sift.CLI.Extensions;

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