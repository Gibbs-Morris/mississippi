using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Default feature-state builder implementation for Reservoir.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public sealed class ReservoirFeatureStateBuilder<TState> : IFeatureStateBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirFeatureStateBuilder{TState}" /> class.
    /// </summary>
    /// <param name="reservoir">The parent builder.</param>
    public ReservoirFeatureStateBuilder(
        ReservoirBuilder reservoir
    ) =>
        Reservoir = reservoir;

    /// <inheritdoc />
    public IReservoirBuilder Reservoir { get; }
}