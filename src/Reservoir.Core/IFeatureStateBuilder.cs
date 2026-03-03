using System;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Builder for configuring a specific feature state, its reducers, and effects.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public interface IFeatureStateBuilder<TState> : IMississippiBuilder
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Gets the CLR type of the feature state being configured.
    /// </summary>
    Type FeatureStateType => typeof(TState);
}