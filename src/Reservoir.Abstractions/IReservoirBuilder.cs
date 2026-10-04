using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Defines the public builder used to configure Reservoir services.
/// </summary>
/// <remarks>
///     Justification: public contract for host and package extensions that compose Reservoir registrations.
/// </remarks>
public interface IReservoirBuilder
{
    /// <summary>
    ///     Gets the underlying service collection for advanced extension scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    IServiceCollection Services { get; }

    /// <summary>
    ///     Adds a feature state to Reservoir without additional reducers or effects.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder AddFeatureState<TState>()
        where TState : class, IFeatureState, new();

    /// <summary>
    ///     Adds a feature state and configures its reducers and effects.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="configure">The feature-state configuration callback.</param>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder AddFeatureState<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new();

    /// <summary>
    ///     Adds a middleware implementation to the Reservoir pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware;
}