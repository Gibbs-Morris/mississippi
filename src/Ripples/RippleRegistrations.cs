using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples;

/// <summary>
///     Provides extension methods for registering Ripples components in the dependency injection container.
/// </summary>
public static class RippleRegistrations
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
        services.AddTransient<DelegateActionReducer<TAction, TState>>(_ => new(reduce));
        services.AddTransient<IActionReducer<TState>>(sp =>
            sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        services.AddTransient<IActionReducer<TAction, TState>>(sp =>
            sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        services.AddRootActionReducer<TState>();
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
        where TReducer : class, IActionReducer<TAction, TState>
    {
        services.AddTransient<IActionReducer<TState>, TReducer>();
        services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        services.AddRootActionReducer<TState>();
        return services;
    }

    /// <summary>
    ///     Adds the RippleStore to the service collection with DI-resolved reducers, effects, and middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRippleStore(
        this IServiceCollection services
    )
    {
        services.TryAddScoped<IRippleStore>(sp => new RippleStore(sp));
        return services;
    }

    /// <summary>
    ///     Adds a root action reducer for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection to add the root reducer to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRootActionReducer<TState>(
        this IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootActionReducer<TState>, RootActionReducer<TState>>();
        return services;
    }
}