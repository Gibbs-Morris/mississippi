using System;

using OpenTelemetry.Metrics;


namespace Mississippi.OpenTelemetry.Extensions;

/// <summary>
///     Extension methods for configuring OpenTelemetry meters for Mississippi framework components.
/// </summary>
public static class MeterBuilderExtensions
{
    /// <summary>
    ///     Adds all Mississippi framework meters to the meter provider.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The meter provider builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is <c>null</c>.</exception>
    /// <remarks>
    ///     <para>
    ///         This method registers meters for all Mississippi components:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description>Aqueduct (SignalR backplane)</description></item>
    ///         <item><description>Aggregates (command execution)</description></item>
    ///         <item><description>Brooks (event streams)</description></item>
    ///         <item><description>Inlet (real-time notifications)</description></item>
    ///         <item><description>Snapshots (caching and persistence)</description></item>
    ///         <item><description>Storage.Locking (distributed locks)</description></item>
    ///         <item><description>Storage.Snapshots (Cosmos DB snapshot storage)</description></item>
    ///         <item><description>UxProjections (projection queries)</description></item>
    ///     </list>
    ///     <para>
    ///         For selective meter registration, use <c>.AddMeter()</c> with individual
    ///         constants from <see cref="MississippiMeters" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics => metrics
    ///         .AddAspNetCoreInstrumentation()
    ///         .AddMississippiMeters()
    ///         .AddMeter("Microsoft.Orleans"));
    ///     </code>
    /// </example>
    public static MeterProviderBuilder AddMississippiMeters(
        this MeterProviderBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddMeter(MississippiMeters.Aqueduct)
            .AddMeter(MississippiMeters.Aggregates)
            .AddMeter(MississippiMeters.Brooks)
            .AddMeter(MississippiMeters.Inlet)
            .AddMeter(MississippiMeters.Snapshots)
            .AddMeter(MississippiMeters.StorageLocking)
            .AddMeter(MississippiMeters.StorageSnapshots)
            .AddMeter(MississippiMeters.UxProjections);
    }
}
