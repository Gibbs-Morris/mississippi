using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.State;

/// <summary>
///     Represents a typed feature state registration that provides the initial state,
///     optional root reducer, and optional root action effect for a specific feature state type.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public sealed class FeatureStateRegistration<TState> : IFeatureStateRegistration
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureStateRegistration{TState}" /> class.
    /// </summary>
    /// <param name="rootReducer">The optional root reducer for processing actions.</param>
    /// <param name="rootActionEffect">The optional root action effect for handling side effects.</param>
    public FeatureStateRegistration(
        IRootReducer<TState>? rootReducer = null,
        IRootActionEffect<TState>? rootActionEffect = null
    )
    {
        RootReducer = rootReducer;
        RootActionEffect = rootActionEffect;
    }

    /// <inheritdoc />
    public string FeatureKey => TState.FeatureKey;

    /// <inheritdoc />
    public object InitialState => new TState();

    /// <inheritdoc />
    public object? RootActionEffect { get; }

    /// <inheritdoc />
    public object? RootReducer { get; }
}