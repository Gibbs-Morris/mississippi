using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotCaching(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ISnapshotGrainFactory, SnapshotGrainFactory>();
        return services;
    }

    /// <summary>
    ///     Registers a snapshot state converter for the specified state type.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to convert.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotStateConverter<TSnapshot>(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>();
        return services;
    }
}