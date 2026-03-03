using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Internal builder for configuring a specific feature state, its reducers, and effects.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
internal sealed class FeatureStateBuilder<TState> : IFeatureStateBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureStateBuilder{TState}" /> class.
    /// </summary>
    /// <param name="services">The parent service collection (shared with <see cref="ReservoirBuilder" />).</param>
    public FeatureStateBuilder(
        IServiceCollection services
    ) =>
        Services = services;

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    private int ReducerCount { get; set; }

    /// <summary>
    ///     Increments the reducer count for validation tracking.
    /// </summary>
    public void IncrementReducerCount() => ReducerCount++;

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate() =>
        ReducerCount > 0
            ? []
            :
            [
                new(
                    "Reservoir.Feature.NoReducersConfigured",
                    "Reservoir",
                    $"No reducers configured for feature state '{typeof(TState).Name}'.",
                    "Call feature.AddReducer<TAction>(...) to register at least one reducer."),
            ];
}