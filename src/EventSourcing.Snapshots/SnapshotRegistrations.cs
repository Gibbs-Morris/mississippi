using System;

using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     Provides extension methods for registering snapshot caching components in the dependency injection container.
/// </summary>
public static class SnapshotRegistrations
{
    /// <summary>
    ///     Adds snapshot caching infrastructure services to the service collection.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSnapshotCaching(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => services.TryAddSingleton<ISnapshotGrainFactory, SnapshotGrainFactory>());
        return builder;
    }

    /// <summary>
    ///     Registers a snapshot state converter for the specified state type.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to convert.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSnapshotStateConverter<TSnapshot>(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>());
        return builder;
    }
}