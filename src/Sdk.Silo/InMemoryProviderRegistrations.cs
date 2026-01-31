using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

using Orleans.Hosting;


namespace Mississippi.Sdk.Silo;

/// <summary>
///     In-memory provider registrations for Orleans silo hosts.
/// </summary>
public static class InMemoryProviderRegistrations
{
    /// <summary>
    ///     Configures in-memory Orleans stream and grain storage providers.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configure">Optional action to configure in-memory provider options.</param>
    /// <returns>The Mississippi silo builder for chaining.</returns>
    public static MississippiSiloBuilder AddInMemoryProviders(
        this MississippiSiloBuilder builder,
        Action<InMemoryProviderOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        InMemoryProviderOptions options = new();
        configure?.Invoke(options);
        builder.Services.AddOptions<BrookProviderOptions>();
        builder.Services.Configure<BrookProviderOptions>(o => o.OrleansStreamProviderName = options.StreamProviderName);
        builder.UseOrleans(siloBuilder =>
        {
            siloBuilder.AddMemoryStreams(options.StreamProviderName);
            siloBuilder.AddMemoryGrainStorage(options.StorageNames.PubSub);
            siloBuilder.AddMemoryGrainStorage(options.StorageNames.EventLog);
            siloBuilder.AddMemoryGrainStorage(options.StorageNames.Snapshots);
            siloBuilder.AddMemoryGrainStorage(options.StorageNames.Projections);
        });
        return builder;
    }
}