using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Util.Logging;

public static class LoggingExtensions
{
    public static IHostBuilder UseStructuredLogging(this IHostBuilder hostBuilder, string serviceName) =>
        hostBuilder.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.With<TraceIdEnricher>()
            .Enrich.WithProperty("service", serviceName)
            .WriteTo.Console(new RenderedCompactJsonFormatter()));
}
