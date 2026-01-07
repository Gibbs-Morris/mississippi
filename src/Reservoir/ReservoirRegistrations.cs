using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Provides extension methods for registering Reservoir components in the dependency injection container.
/// </summary>
public static class ReservoirRegistrations
{
    /// <summary>
    ///     Adds an effect implementation to the service collection.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <param name="services">The service collection to add the effect to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddEffect<TEffect>(
        this IServiceCollection services
    )
        where TEffect : class, IEffect
    {
        services.AddTransient<IEffect, TEffect>();
        return services;
    }

    /// <summary>
    ///     Adds a middleware implementation to the service collection.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware implementation type.</typeparam>
    /// <param name="services">The service collection to add the middleware to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMiddleware<TMiddleware>(
        this IServiceCollection services
    )
        where TMiddleware : class, IMiddleware
    {
        services.AddTransient<IMiddleware, TMiddleware>();
        return services;
    }

    /// <summary>
    ///     Adds a reducer expressed as a delegate to the service collection.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the reducer.</typeparam>
    /// <param name="services">The service collection to add the reducer to.</param>
    /// <param name="reduce">The delegate invoked to apply actions to the current state.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TAction, TState>(
        this IServiceCollection services,
        Func<TState, TAction, TState> reduce
    )
        where TAction : IAction
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(reduce);
        services.AddTransient<DelegateReducer<TAction, TState>>(_ => new(reduce));
        services.AddTransient<IReducer<TState>>(sp => sp.GetRequiredService<DelegateReducer<TAction, TState>>());
        services.AddTransient<IReducer<TAction, TState>>(sp =>
            sp.GetRequiredService<DelegateReducer<TAction, TState>>());
        services.AddRootReducer<TState>();
        return services;
    }

    /// <summary>
    ///     Adds a reducer implementation to the service collection.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the reducer.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="services">The service collection to add the reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReducer<TAction, TState, TReducer>(
        this IServiceCollection services
    )
        where TAction : IAction
        where TState : class, IFeatureState, new()
        where TReducer : class, IReducer<TAction, TState>
    {
        services.AddTransient<IReducer<TState>, TReducer>();
        services.AddTransient<IReducer<TAction, TState>, TReducer>();
        services.AddRootReducer<TState>();
        return services;
    }

    /// <summary>
    ///     Adds the Store to the service collection with DI-resolved reducers, effects, and middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStore">Optional action to configure the store with feature states.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReservoir(
        this IServiceCollection services,
        Action<Store>? configureStore = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IStore>(sp =>
        {
            Store store = new(sp);
            configureStore?.Invoke(store);
            return store;
        });
        return services;
    }

    /// <summary>
    ///     Adds a root reducer for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection to add the root reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRootReducer<TState>(
        this IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>();
        return services;
    }
}