using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Test feature state used by <see cref="ReservoirSceneBaseBehaviorTests" />.
/// </summary>
internal sealed record TestReservoirFeatureState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "test-reservoir-scene";

    /// <summary>
    ///     Gets the counter value used by the tests.
    /// </summary>
    public int Counter { get; init; }
}
