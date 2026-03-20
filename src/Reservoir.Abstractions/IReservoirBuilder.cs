using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Builder contract for composing Reservoir features before attaching them to a host.
/// </summary>
public interface IReservoirBuilder
{
    /// <summary>
    ///     Adds a feature state to the Reservoir composition and configures it.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="configure">The feature configuration callback.</param>
    /// <returns>The same builder instance for chaining.</returns>
    IReservoirBuilder AddFeature<TState>(
        Action<IFeatureStateBuilder<TState>> configure
    )
        where TState : class, IFeatureState, new();

    /// <summary>
    ///     Adds a feature state to the Reservoir composition without additional configuration.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The same builder instance for chaining.</returns>
    IReservoirBuilder AddFeature<TState>()
        where TState : class, IFeatureState, new();

    /// <summary>
    ///     Validates the current builder configuration.
    /// </summary>
    void Validate();
}