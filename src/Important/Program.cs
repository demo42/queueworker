using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Important
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                    config.AddJsonFile("config.json")
                          .AddEnvironmentVariables())
                .ConfigureLogging(log => log.AddConsole())
                .ConfigureServices(services => services.AddSingleton<IHostedService, QueueProcessorService>());

            var configPath = Environment.GetEnvironmentVariable("ConfigPath");
            if (!string.IsNullOrEmpty(configPath))
            {
                builder.ConfigureAppConfiguration(config => config.AddKeyPerFile(configPath, true));
            }

            await builder.Build().RunAsync();
        }
    }
}
