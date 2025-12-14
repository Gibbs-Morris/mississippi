using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Provides extension methods for registering reducers in the dependency injection container.
/// </summary>
public static class ReducerRegistrations
{
    /// <summary>
    ///     Adds a reducer expressed as a delegate to the service collection.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
    /// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
    /// <param name="services">The service collection to add the reducer to.</param>
    /// <param name="reduce">The delegate invoked to apply events to the current projection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TEvent, TProjection>(
        this IServiceCollection services,
        Func<TProjection, TEvent, TProjection> reduce
    )
    {
        ArgumentNullException.ThrowIfNull(reduce);
        services.AddTransient<DelegateReducer<TEvent, TProjection>>(_ => new(reduce));
        services.AddTransient<IReducer<TProjection>>(sp =>
            sp.GetRequiredService<DelegateReducer<TEvent, TProjection>>());
        services.AddTransient<IReducer<TEvent, TProjection>>(sp =>
            sp.GetRequiredService<DelegateReducer<TEvent, TProjection>>());
        services.AddRootReducer<TProjection>();
        return services;
    }

    /// <summary>
    ///     Adds a reducer implementation to the service collection.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
    /// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="services">The service collection to add the reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TEvent, TProjection, TReducer>(
        this IServiceCollection services
    )
        where TReducer : class, IReducer<TEvent, TProjection>
    {
        services.AddTransient<IReducer<TProjection>, TReducer>();
        services.AddTransient<IReducer<TEvent, TProjection>, TReducer>();
        services.AddRootReducer<TProjection>();
        return services;
    }

    /// <summary>
    ///     Adds a root reducer for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="services">The service collection to add the root reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRootReducer<TProjection>(
        this IServiceCollection services
    )
    {
        services.TryAddTransient<IRootReducer<TProjection>, RootReducer<TProjection>>();
        return services;
    }
}