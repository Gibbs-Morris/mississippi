using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     OpenTelemetry configuration for Spring silo.
/// </summary>
internal static class SpringObservabilityRegistrations
{
    /// <summary>
    ///     Adds OpenTelemetry tracing, metrics, and logging for the Spring silo.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddSpringObservability(
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

            // Mississippi framework meters
            .AddMeter("Mississippi.EventSourcing.Brooks")
            .AddMeter("Mississippi.EventSourcing.Aggregates")
            .AddMeter("Mississippi.EventSourcing.Snapshots")
            .AddMeter("Mississippi.Storage.Cosmos")
            .AddMeter("Mississippi.Storage.Snapshots")
            .AddMeter("Mississippi.Storage.Locking")

            // Orleans meters
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