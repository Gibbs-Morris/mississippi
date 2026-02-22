using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Provides extension methods for registering reducers in the dependency injection container.
/// </summary>
public static class ReducerRegistrations
{
    /// <summary>
    ///     Adds an event reducer expressed as a delegate to the service collection.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the event reducer.</typeparam>
    /// <typeparam name="TProjection">The projection type produced by the event reducer.</typeparam>
    /// <param name="services">The service collection to add the event reducer to.</param>
    /// <param name="reduce">The delegate invoked to apply events to the current projection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TEvent, TProjection>(
        this IServiceCollection services,
        Func<TProjection, TEvent, TProjection> reduce
    )
    {
        ArgumentNullException.ThrowIfNull(reduce);
        services.AddTransient<DelegateEventReducer<TEvent, TProjection>>(sp => new(
            reduce,
            sp.GetService<ILogger<DelegateEventReducer<TEvent, TProjection>>>()));
        services.AddTransient<IEventReducer<TProjection>>(sp =>
            sp.GetRequiredService<DelegateEventReducer<TEvent, TProjection>>());
        services.AddTransient<IEventReducer<TEvent, TProjection>>(sp =>
            sp.GetRequiredService<DelegateEventReducer<TEvent, TProjection>>());
        services.AddRootReducer<TProjection>();
        return services;
    }

    /// <summary>
    ///     Adds an event reducer implementation to the service collection.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the event reducer.</typeparam>
    /// <typeparam name="TProjection">The projection type produced by the event reducer.</typeparam>
    /// <typeparam name="TReducer">The event reducer implementation type.</typeparam>
    /// <param name="services">The service collection to add the event reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TEvent, TProjection, TReducer>(
        this IServiceCollection services
    )
        where TReducer : class, IEventReducer<TEvent, TProjection>
    {
        services.AddTransient<IEventReducer<TProjection>, TReducer>();
        services.AddTransient<IEventReducer<TEvent, TProjection>, TReducer>();
        services.AddRootReducer<TProjection>();
        return services;
    }

    /// <summary>
    ///     Adds a root event reducer for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="services">The service collection to add the root event reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRootReducer<TProjection>(
        this IServiceCollection services
    )
    {
        services.TryAddTransient<IRootReducer<TProjection>, RootReducer<TProjection>>();
        return services;
    }
}