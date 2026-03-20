using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Core Reservoir builder extensions for feature state, reducers, effects, and middleware.
/// </summary>
public static class ReservoirBuilderCoreExtensions
{
    /// <summary>
    ///     Adds a state-scoped action effect implementation to the feature state.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <typeparam name="TEffect">The action effect type.</typeparam>
    /// <param name="feature">The feature builder.</param>
    /// <returns>The feature builder for chaining.</returns>
    public static IFeatureStateBuilder<TState> AddActionEffect<TState, TEffect>(
        this IFeatureStateBuilder<TState> feature
    )
        where TState : class, IFeatureState, new()
        where TEffect : class, IActionEffect<TState>
    {
        ReservoirFeatureStateBuilder<TState> builder = ReservoirServiceRegistrationHelpers.GetFeatureBuilder(feature);
        builder.ReservoirBuilder().Services.AddTransient<IActionEffect<TState>, TEffect>();
        ReservoirServiceRegistrationHelpers.AddRootActionEffect<TState>(builder.ReservoirBuilder().Services);
        return feature;
    }

    /// <summary>
    ///     Adds a middleware implementation to the Reservoir pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddMiddleware<TMiddleware>(
        this IReservoirBuilder reservoir
    )
        where TMiddleware : class, IMiddleware
    {
        ReservoirBuilder builder = ReservoirServiceRegistrationHelpers.GetBuilder(reservoir);
        builder.Services.AddTransient<IMiddleware, TMiddleware>();
        return reservoir;
    }

    /// <summary>
    ///     Adds a delegate-based reducer to the feature state.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <param name="feature">The feature builder.</param>
    /// <param name="reduce">The reducer delegate.</param>
    /// <returns>The feature builder for chaining.</returns>
    public static IFeatureStateBuilder<TState> AddReducer<TState, TAction>(
        this IFeatureStateBuilder<TState> feature,
        Func<TState, TAction, TState> reduce
    )
        where TState : class, IFeatureState, new()
        where TAction : IAction
    {
        ArgumentNullException.ThrowIfNull(reduce);
        ReservoirFeatureStateBuilder<TState> builder = ReservoirServiceRegistrationHelpers.GetFeatureBuilder(feature);
        builder.ReservoirBuilder().Services.AddTransient<DelegateActionReducer<TAction, TState>>(_ => new(reduce));
        builder.ReservoirBuilder()
            .Services.AddTransient<IActionReducer<TState>>(sp =>
                sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        builder.ReservoirBuilder()
            .Services.AddTransient<IActionReducer<TAction, TState>>(sp =>
                sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        ReservoirServiceRegistrationHelpers.AddRootReducer<TState>(builder.ReservoirBuilder().Services);
        return feature;
    }

    /// <summary>
    ///     Adds a reducer implementation type to the feature state.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="feature">The feature builder.</param>
    /// <returns>The feature builder for chaining.</returns>
    public static IFeatureStateBuilder<TState> AddReducer<TState, TAction, TReducer>(
        this IFeatureStateBuilder<TState> feature
    )
        where TState : class, IFeatureState, new()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>
    {
        ReservoirFeatureStateBuilder<TState> builder = ReservoirServiceRegistrationHelpers.GetFeatureBuilder(feature);
        builder.ReservoirBuilder().Services.AddTransient<IActionReducer<TState>, TReducer>();
        builder.ReservoirBuilder().Services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        ReservoirServiceRegistrationHelpers.AddRootReducer<TState>(builder.ReservoirBuilder().Services);
        return feature;
    }

    private static ReservoirBuilder ReservoirBuilder<TState>(
        this ReservoirFeatureStateBuilder<TState> feature
    )
        where TState : class, IFeatureState, new() =>
        (ReservoirBuilder)feature.Reservoir;
}