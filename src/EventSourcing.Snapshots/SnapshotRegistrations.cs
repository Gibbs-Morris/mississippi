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
    ///     Registers an initial state factory for the specified snapshot type.
    /// </summary>
    /// <typeparam name="TSnapshot">The snapshot type.</typeparam>
    /// <typeparam name="TFactory">The factory implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         The factory is used by <see cref="SnapshotCacheGrain{TSnapshot}" /> to create
    ///         the initial state when no events have been processed yet.
    ///     </para>
    ///     <para>
    ///         With this new design, you no longer need to create concrete snapshot cache grain
    ///         classes that inherit from a base class. Instead, just register the initial state
    ///         factory and the framework provides a sealed, generic snapshot cache grain.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInitialStateFactory<TSnapshot, TFactory>(
        this IServiceCollection services
    )
        where TFactory : class, IInitialStateFactory<TSnapshot>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IInitialStateFactory<TSnapshot>, TFactory>();
        return services;
    }

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