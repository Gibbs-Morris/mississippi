using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Observability;

/// <summary>
///     Extension methods for registering Mississippi observability services.
/// </summary>
public static class ObservabilityRegistrations
{
    /// <summary>
    ///     Adds Mississippi observability services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers marker services that indicate Mississippi telemetry is enabled.
    ///         Consumer applications should additionally configure OpenTelemetry export by calling:
    ///     </para>
    ///     <code>
    ///     services.AddOpenTelemetry()
    ///         .WithTracing(tracing =&gt; tracing.AddSource(MississippiActivitySources.All.ToArray()))
    ///         .WithMetrics(metrics =&gt; metrics.AddMeter(MississippiMeters.All.ToArray()));
    ///     </code>
    ///     <para>
    ///         The framework instrumentation code checks for listener presence before emitting spans
    ///         and metrics, so no overhead is incurred if collectors are not configured.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddMississippiObservability(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register a marker to indicate observability is configured
        services.AddSingleton<MississippiObservabilityMarker>();

        return services;
    }

    /// <summary>
    ///     Marker class indicating Mississippi observability has been registered.
    ///     This class intentionally has no members - it serves as a registration marker.
    /// </summary>
#pragma warning disable S2094 // Intentional marker class for DI registration detection
    internal sealed class MississippiObservabilityMarker;
#pragma warning restore S2094
}
