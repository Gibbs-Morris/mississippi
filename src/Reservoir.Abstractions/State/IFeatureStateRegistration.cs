namespace Mississippi.Reservoir.Abstractions.State;

/// <summary>
///     Represents a feature state registration that provides the initial state
///     and optional root reducer for a feature state type.
/// </summary>
/// <remarks>
///     <para>
///         Feature state registrations are collected by the store at construction time.
///         Each registration provides the feature key, initial state instance, and
///         optionally a root reducer for processing actions.
///     </para>
///     <para>
///         This abstraction enables declarative registration of feature states in DI
///         without requiring the store to use service locator patterns.
///     </para>
/// </remarks>
public interface IFeatureStateRegistration
{
    /// <summary>
    ///     Gets the unique key identifying the feature state.
    /// </summary>
    string FeatureKey { get; }

    /// <summary>
    ///     Gets the initial state instance for this feature.
    /// </summary>
    object InitialState { get; }

    /// <summary>
    ///     Gets the root reducer for this feature state, or null if no reducers are registered.
    /// </summary>
    /// <remarks>
    ///     When present, this is an <see cref="IRootReducer{TState}" /> instance boxed as object.
    /// </remarks>
    object? RootReducer { get; }
}
