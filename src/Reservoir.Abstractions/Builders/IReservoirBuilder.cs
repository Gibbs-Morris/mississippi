using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions.Builders;

/// <summary>
///     Builder contract for Reservoir registration.
/// </summary>
public interface IReservoirBuilder
{
    /// <summary>
    ///     Adds a Reservoir feature for the specified state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="configure">The feature builder configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder AddFeature<TState>(
        Action<IReservoirFeatureBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new();

    /// <summary>
    ///     Adds a middleware implementation.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware;

    /// <summary>
    ///     Configures services for the builder.
    /// </summary>
    /// <param name="configure">The services configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IReservoirBuilder ConfigureServices(
        Action<IServiceCollection> configure
    );
}