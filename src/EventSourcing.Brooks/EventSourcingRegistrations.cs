using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Reader;


namespace Mississippi.EventSourcing.Brooks;

/// <summary>
///     Extension methods for registering EventSourcing services in dependency injection.
/// </summary>
public static class EventSourcingRegistrations
{
    /// <summary>
    ///     Configures Orleans silo to support event sourcing grains.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configureOptions">
    ///     Optional action to configure <see cref="BrookProviderOptions" />.
    ///     Use this to specify which stream provider name Brooks should use.
    /// </param>
    /// <returns>The modified silo builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method does NOT configure streams or storage - the host application
    ///         is responsible for that. Use <paramref name="configureOptions" /> to tell
    ///         Brooks which stream provider to use:
    ///     </para>
    ///     <code>
    ///     // Host configures infrastructure
    ///     siloBuilder.AddMemoryStreams("MyStreams");
    ///     siloBuilder.AddMemoryGrainStorage("PubSubStore");
    ///
    ///     // Tell Brooks which stream provider to use
    ///     IMississippiSiloBuilder mississippi = siloBuilder.AddMississippiSilo();
    ///     mississippi.AddEventSourcing(options =&gt;
    ///         options.OrleansStreamProviderName = "MyStreams");
    ///     </code>
    /// </remarks>
    public static IMississippiSiloBuilder AddEventSourcing(
        this IMississippiSiloBuilder builder,
        Action<BrookProviderOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            AddEventSourcingServices(services);
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
        });
        return builder;
    }

    private static void AddEventSourcingServices(
        IServiceCollection services
    )
    {
        // Register the grain factory for accessing Orleans grains
        // Register as singleton and expose both public (abstractions) and internal interfaces
        services.AddSingleton<BrookGrainFactory>();
        services.AddSingleton<IBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.AddSingleton<IInternalBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());

        // Register the stream ID factory for Orleans streams
        services.AddSingleton<IStreamIdFactory, StreamIdFactory>();

        // Register options for brook reader and provider
        services.AddOptions<BrookReaderOptions>();
        services.AddOptions<BrookProviderOptions>();
    }
}