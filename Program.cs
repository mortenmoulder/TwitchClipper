using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;
using TwitchClipper.Helpers;
using TwitchClipper.Models;
using TwitchClipper.Services;

namespace TwitchClipper
{
    public class Program
    {
        public static IConfiguration Configuration { get; private set; }

        public static async Task Main(string[] args)
        {
            try
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .Build();

                var services = ConfigureServices();
                var serviceProvider = services.BuildServiceProvider();

                Options options = null;

                Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
                {
                    options = o;
                }).WithNotParsed(errors =>
                {
                    Environment.Exit(-1);
                });

                Console.Clear();

                await LogHelper.Log($"Downloading clips made by {options.Username}");

                await serviceProvider.GetService<Application>().Run(options);

                await LogHelper.Log("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(Configuration);
            services.AddOptions();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            services.AddTransient<Application>();
            services.AddTransient<ITwitchAPIService, TwitchAPIService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<ITwitchConfigurationService, TwitchConfigurationService>();
            services.AddTransient<IYouTubeDLService, YouTubeDLService>();
            services.AddScoped<IHostService, HostService>();

            return services;
        }
    }
}
