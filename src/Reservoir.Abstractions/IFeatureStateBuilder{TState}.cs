using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Builder contract for configuring a specific Reservoir feature state.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public interface IFeatureStateBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Gets the parent Reservoir builder.
    /// </summary>
    IReservoirBuilder Reservoir { get; }

    /// <summary>
    ///     Gets the feature state type configured by this builder.
    /// </summary>
    Type StateType => typeof(TState);
}