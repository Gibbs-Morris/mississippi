using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Testing;

/// <summary>
///     Factory methods for creating test harnesses.
/// </summary>
public static class StoreTestHarnessFactory
{
    /// <summary>
    ///     Creates a new test harness for the specified feature state type.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>A new test harness.</returns>
    public static StoreTestHarness<TState> ForFeature<TState>()
        where TState : class, IFeatureState, new() =>
        new();
}