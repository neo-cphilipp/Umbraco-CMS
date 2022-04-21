using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Infrastructure.Logging.Serilog;
using Umbraco.Cms.Web.Common.DependencyInjection;
using Umbraco.Extensions;

namespace Umbraco.Cms.Web.Common.Hosting
{
    /// <summary>
    /// Umbraco specific extensions for the <see cref="IHostBuilder"/> interface.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures an existing <see cref="IHostBuilder"/> with defaults for an Umbraco application.
        /// </summary>
        public static IHostBuilder ConfigureUmbracoDefaults(this IHostBuilder builder)
        {
#if DEBUG
            builder.ConfigureAppConfiguration(config
                => config.AddJsonFile(
                    "appsettings.Local.json",
                    optional: true,
                    reloadOnChange: true));

#endif
            builder.ConfigureLogging(x => x.ClearProviders());

            builder.ConfigureServices((context, services) =>
            {
                services.AddLogger(context.HostingEnvironment, context.Configuration, bootstrapConfig =>
                {
                    Log.Logger = bootstrapConfig.CreateBootstrapLogger();
                });
            });

            builder.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimalConfiguration(
                        context.HostingEnvironment,
                        services.GetRequiredService<ILoggingConfiguration>(),
                        services.GetRequiredService<UmbracoFileConfiguration>())
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services); // Adds ILogEventEnricher found in container.
            });

            return new UmbracoHostBuilderDecorator(builder, OnHostBuilt);
        }

        // Runs before any IHostedService starts (including generic web host).
        private static void OnHostBuilt(IHost host) =>
            StaticServiceProvider.Instance = host.Services;
    }
}
