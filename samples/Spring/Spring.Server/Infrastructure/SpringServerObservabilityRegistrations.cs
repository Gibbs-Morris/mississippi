using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;


namespace Spring.Server.Infrastructure;

/// <summary>
///     OpenTelemetry configuration for Spring server.
/// </summary>
internal static class SpringServerObservabilityRegistrations
{
    /// <summary>
    ///     Adds OpenTelemetry tracing, metrics, and logging for the Spring server.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddSpringServerObservability(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(ConfigureTracing)
            .WithMetrics(ConfigureMetrics)
            .WithLogging()
            .UseOtlpExporter();
        return builder;
    }

    private static void ConfigureMetrics(
        MeterProviderBuilder metrics
    )
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("Microsoft.Orleans");
    }

    private static void ConfigureTracing(
        TracerProviderBuilder tracing
    )
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Microsoft.Orleans.Runtime")
            .AddSource("Microsoft.Orleans.Application");
    }
}