using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Aqueduct.Grains;

/// <summary>
///     Extension methods for configuring Aqueduct on Orleans silos.
/// </summary>
public static class AqueductSiloBuilderExtensions
{
    /// <summary>
    ///     Configures Aqueduct for SignalR backplane support on the silo.
    /// </summary>
    /// <param name="siloBuilder">The silo builder to configure.</param>
    /// <param name="configureOptions">An action to configure <see cref="AqueductSiloOptions" />.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the Aqueduct grains and configures the SignalR options.
    ///         Use the <see cref="AqueductSiloOptions" /> to configure stream providers:
    ///     </para>
    ///     <code>
    ///     // Basic setup - configure streams yourself
    ///     siloBuilder.UseAqueduct(options =>
    ///     {
    ///         options.StreamProviderName = "MyStreams";
    ///     });
    ///
    ///     // Development/testing with in-memory streams
    ///     siloBuilder.UseAqueduct(options =>
    ///     {
    ///         options.UseMemoryStreams();
    ///     });
    ///
    ///     // Production with custom stream configuration
    ///     siloBuilder.UseAqueduct(options =>
    ///     {
    ///         options.UseMemoryStreams("CustomStreamProvider", "CustomPubSubStore");
    ///     });
    ///     </code>
    /// </remarks>
    public static ISiloBuilder UseAqueduct(
        this ISiloBuilder siloBuilder,
        Action<AqueductSiloOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ArgumentNullException.ThrowIfNull(configureOptions);
        AqueductSiloOptions options = new(siloBuilder);
        configureOptions(options);

        // Register the Orleans SignalR options
        siloBuilder.Services.Configure<OrleansSignalROptions>(orleanOptions =>
        {
            orleanOptions.StreamProviderName = options.StreamProviderName;
            orleanOptions.ServerStreamNamespace = options.ServerStreamNamespace;
            orleanOptions.AllClientsStreamNamespace = options.AllClientsStreamNamespace;
            orleanOptions.HeartbeatIntervalMinutes = options.HeartbeatIntervalMinutes;
            orleanOptions.DeadServerTimeoutMultiplier = options.DeadServerTimeoutMultiplier;
        });
        return siloBuilder;
    }

    /// <summary>
    ///     Configures Aqueduct for SignalR backplane support on the silo with default options.
    /// </summary>
    /// <param name="siloBuilder">The silo builder to configure.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This overload uses default <see cref="OrleansSignalROptions" /> values.
    ///         You must configure the stream provider separately with the name "SignalRStreams".
    ///     </para>
    /// </remarks>
    public static ISiloBuilder UseAqueduct(
        this ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        return siloBuilder.UseAqueduct(_ => { });
    }
}