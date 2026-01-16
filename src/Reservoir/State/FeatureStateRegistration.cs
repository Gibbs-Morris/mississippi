using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.State;

/// <summary>
///     Represents a typed feature state registration that provides the initial state
///     and optional root reducer for a specific feature state type.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public sealed class FeatureStateRegistration<TState> : IFeatureStateRegistration
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureStateRegistration{TState}" /> class.
    /// </summary>
    /// <param name="rootReducer">The optional root reducer for processing actions.</param>
    public FeatureStateRegistration(
        IRootReducer<TState>? rootReducer = null
    )
    {
        RootReducer = rootReducer;
    }

    /// <inheritdoc />
    public string FeatureKey => TState.FeatureKey;

    /// <inheritdoc />
    public object InitialState => new TState();

    /// <inheritdoc />
    public object? RootReducer { get; }
}
