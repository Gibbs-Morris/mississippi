using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Builders;
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
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="reduce">The delegate invoked to apply events to the current projection.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddReducer<TEvent, TProjection>(
        this IMississippiSiloBuilder builder,
        Func<TProjection, TEvent, TProjection> reduce
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(reduce);
        builder.ConfigureServices(services => AddReducerServices(services, reduce));
        return builder;
    }

    /// <summary>
    ///     Adds an event reducer implementation to the service collection.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the event reducer.</typeparam>
    /// <typeparam name="TProjection">The projection type produced by the event reducer.</typeparam>
    /// <typeparam name="TReducer">The event reducer implementation type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddReducer<TEvent, TProjection, TReducer>(
        this IMississippiSiloBuilder builder
    )
        where TReducer : class, IEventReducer<TEvent, TProjection>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddReducerServices<TEvent, TProjection, TReducer>(services));
        return builder;
    }

    /// <summary>
    ///     Adds a root event reducer for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddRootReducer<TProjection>(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => AddRootReducerServices<TProjection>(services));
        return builder;
    }

    private static void AddReducerServices<TEvent, TProjection>(
        IServiceCollection services,
        Func<TProjection, TEvent, TProjection> reduce
    )
    {
        services.AddTransient<DelegateEventReducer<TEvent, TProjection>>(sp => new(
            reduce,
            sp.GetService<ILogger<DelegateEventReducer<TEvent, TProjection>>>()));
        services.AddTransient<IEventReducer<TProjection>>(sp =>
            sp.GetRequiredService<DelegateEventReducer<TEvent, TProjection>>());
        services.AddTransient<IEventReducer<TEvent, TProjection>>(sp =>
            sp.GetRequiredService<DelegateEventReducer<TEvent, TProjection>>());
        AddRootReducerServices<TProjection>(services);
    }

    private static void AddReducerServices<TEvent, TProjection, TReducer>(
        IServiceCollection services
    )
        where TReducer : class, IEventReducer<TEvent, TProjection>
    {
        services.AddTransient<IEventReducer<TProjection>, TReducer>();
        services.AddTransient<IEventReducer<TEvent, TProjection>, TReducer>();
        AddRootReducerServices<TProjection>(services);
    }

    private static void AddRootReducerServices<TProjection>(
        IServiceCollection services
    )
    {
        services.TryAddTransient<IRootReducer<TProjection>, RootReducer<TProjection>>();
    }
}