using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FileCleanup
{
    /// <summary>
    /// Main program class.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logFile.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                RunAsync().Wait();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Runs the policy service, enforcing the cleanup policies asyncrhonously.
        /// </summary>
        /// <returns>Returns a <see cref="Task"/> for the operation.</returns>
        static async Task RunAsync()
        {
            Log.Information("Creating collection of services.");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var configurationFile = "appsettings.json";
            Log.Information($"Building configuration root from {configurationFile}.");
            var configurationRoot = new ConfigurationBuilder()
                .AddJsonFile(configurationFile, false, true)
                .Build();

            var policyConfigurationSection = "policyConfiguration";
            Log.Information($"Getting {policyConfigurationSection} section.");
            var policyConfiguration = configurationRoot
                .GetSection("policyConfiguration")
                .Get<PolicyConfiguration>();

            Log.Information("Building service provider.");
            var serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Information("Starting policy service.");
                await serviceProvider.GetRequiredService<PolicyService>().EnforcePoliciesAsync(policyConfiguration);
                Log.Information("Ending policy service.");
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Fatal error running policy service.");
                throw exception;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Configures the collection of services.
        /// </summary>
        /// <param name="serviceCollection">Implementation of <see cref="IServiceCollection"/> interface.</param>
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            }));
            serviceCollection.AddLogging();

            serviceCollection.AddSingleton<PolicyService>();
        }
    }
}
