using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.Core.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Provides internal service-registration helpers that back the public Reservoir builder API.
/// </summary>
internal static class ReservoirBuilderRegistrations
{
    /// <summary>
    ///     Adds a state-scoped action effect implementation.
    /// </summary>
    /// <typeparam name="TState">The feature state type this effect operates on.</typeparam>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddActionEffect<TState, TEffect>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
        where TEffect : class, IActionEffect<TState>
    {
        services.AddTransient<IActionEffect<TState>, TEffect>();
        AddRootActionEffect<TState>(services);
        AddFeatureState<TState>(services);
        return services;
    }

    /// <summary>
    ///     Adds a feature state registration.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddFeatureState<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>())));
        return services;
    }

    /// <summary>
    ///     Adds a middleware implementation.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware implementation type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddMiddleware<TMiddleware>(
        IServiceCollection services
    )
        where TMiddleware : class, IMiddleware
    {
        services.AddTransient<IMiddleware, TMiddleware>();
        return services;
    }

    /// <summary>
    ///     Adds an action reducer expressed as a delegate.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the action reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="reduce">The delegate invoked to apply actions to the current state.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddReducer<TAction, TState>(
        IServiceCollection services,
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
        AddRootReducer<TState>(services);
        AddFeatureState<TState>(services);
        return services;
    }

    /// <summary>
    ///     Adds an action reducer implementation.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the action reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
    /// <typeparam name="TReducer">The action reducer implementation type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddReducer<TAction, TState, TReducer>(
        IServiceCollection services
    )
        where TAction : IAction
        where TState : class, IFeatureState, new()
        where TReducer : class, IActionReducer<TAction, TState>
    {
        services.AddTransient<IActionReducer<TState>, TReducer>();
        services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        AddRootReducer<TState>(services);
        AddFeatureState<TState>(services);
        return services;
    }

    /// <summary>
    ///     Adds a root action effect for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddRootActionEffect<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootActionEffect<TState>, RootActionEffect<TState>>();
        return services;
    }

    /// <summary>
    ///     Adds a root action reducer for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddRootReducer<TState>(
        IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>();
        return services;
    }
}