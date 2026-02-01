using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Provides extension methods for registering Reservoir components in the dependency injection container.
/// </summary>
public static class ReservoirRegistrations
{
    /// <summary>
    ///     Adds a state-scoped action effect implementation to the service collection.
    /// </summary>
    /// <typeparam name="TState">The feature state type this effect operates on.</typeparam>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <param name="services">The service collection to add the effect to.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     State-scoped effects are registered with transient lifetime and composited
    ///     into a <see cref="IRootActionEffect{TState}" /> for each feature state.
    /// </remarks>
    public static IServiceCollection AddActionEffect<TState, TEffect>(
        this IServiceCollection services
    )
        where TState : class, IFeatureState, new()
        where TEffect : class, IActionEffect<TState>
    {
        services.AddTransient<IActionEffect<TState>, TEffect>();
        services.AddRootActionEffect<TState>();
        services.AddFeatureState<TState>();
        return services;
    }

    /// <summary>
    ///     Adds a feature state registration to the service collection.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection to add the feature state to.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     This method is called automatically by
    ///     <see cref="AddReducer{TAction,TState}(IServiceCollection, Func{TState,TAction,TState})" />.
    ///     Call it directly only for feature states without reducers.
    /// </remarks>
    public static IServiceCollection AddFeatureState<TState>(
        this IServiceCollection services
    )
        where TState : class, IFeatureState, new()
    {
        // Use TryAddEnumerable with concrete implementation type to prevent duplicate registrations
        // The implementation type (FeatureStateRegistration<TState>) is used for deduplication
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>())));
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
    ///     Adds an action reducer expressed as a delegate to the service collection.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the action reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
    /// <param name="services">The service collection to add the action reducer to.</param>
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
        services.AddRootReducer<TState>();
        services.AddFeatureState<TState>();
        return services;
    }

    /// <summary>
    ///     Adds an action reducer implementation to the service collection.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the action reducer.</typeparam>
    /// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
    /// <typeparam name="TReducer">The action reducer implementation type.</typeparam>
    /// <param name="services">The service collection to add the action reducer to.</param>
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
        services.AddRootReducer<TState>();
        services.AddFeatureState<TState>();
        return services;
    }

    /// <summary>
    ///     Adds the Store to the service collection with DI-resolved feature states and middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     All effects are feature-scoped via <see cref="IActionEffect{TState}" /> and are
    ///     resolved through feature state registrations. There are no global effects.
    /// </remarks>
    public static IServiceCollection AddReservoir(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IStore>(sp => new Store(
            sp.GetServices<IFeatureStateRegistration>(),
            sp.GetServices<IMiddleware>(),
            sp.GetRequiredService<TimeProvider>()));
        return services;
    }

    /// <summary>
    ///     Adds a root action effect for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="services">The service collection to add the root action effect to.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    ///     This method is called automatically by
    ///     <see cref="AddActionEffect{TState, TEffect}" />.
    ///     Call it directly only when manually compositing effects.
    /// </remarks>
    public static IServiceCollection AddRootActionEffect<TState>(
        this IServiceCollection services
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
    /// <param name="services">The service collection to add the root action reducer to.</param>
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