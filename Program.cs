using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
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
            Console.Clear();

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

                var result = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async o =>
                {
                    await Task.Run(() => options = o);
                });

                await result.WithNotParsedAsync(async errors =>
                {
                    await Task.Run(() => Environment.Exit(-1));
                });

                if (!string.IsNullOrWhiteSpace(options.DateFrom) || !string.IsNullOrWhiteSpace(options.DateTo))
                {
                    var filtering = await CreateFiltering(options);

                    await serviceProvider.GetService<IFilteringService>().SetFiltering(filtering);
                }

                await LogHelper.Log($"Downloading clips made by {options.Username}");

                await serviceProvider.GetService<Application>().Run(options);

                await LogHelper.Log("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task<Filtering> CreateFiltering(Options o)
        {
            //if from is set, but to is not, and wise versa
            if ((!string.IsNullOrWhiteSpace(o.DateFrom) && string.IsNullOrWhiteSpace(o.DateTo)) || (!string.IsNullOrWhiteSpace(o.DateTo) && string.IsNullOrWhiteSpace(o.DateFrom)))
            {
                await ErrorHelper.LogAndExit("If you specify --from or --to, the other one has to be present as well");
            }

            var filter = new Filtering();
            DateTime dateFrom, dateTo;

            if (!DateTime.TryParseExact(o.DateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateFrom))
            {
                await ErrorHelper.LogAndExit($"Unable to parse {o.DateFrom} to a date. You must specify the dates as yyyy-MM-dd (e.g. 2021-05-15");
            }

            if (!DateTime.TryParseExact(o.DateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTo))
            {
                await ErrorHelper.LogAndExit($"Unable to parse {o.DateTo} to a date. You must specify the dates as yyyy-MM-dd (e.g. 2021-05-15");
            }

            if (dateTo <= dateFrom)
            {
                await ErrorHelper.LogAndExit("To date must be after from date");
            }

            if (dateTo > DateTime.Today)
            {
                await ErrorHelper.LogAndExit("To date cannot be in the future");
            }

            if (dateFrom < new DateTime(2016, 05, 26))
            {
                await ErrorHelper.LogAndExit("Date from cannot be before 2016-05-26 because that's when Twitch announced clips");
            }

            filter.DateFrom = dateFrom;
            filter.DateTo = dateTo;

            return filter;
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
            services.AddSingleton<IFilteringService, FilteringService>();

            return services;
        }
    }
}
