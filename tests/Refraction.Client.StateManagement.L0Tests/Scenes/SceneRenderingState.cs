using Mississippi.Reservoir.Abstractions.State;


namespace MississippiTests.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Provides immutable state for scene rendering tests.
/// </summary>
internal sealed record SceneRenderingState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "scene-rendering";

    /// <summary>
    ///     Gets the displayed counter.
    /// </summary>
    public int Counter { get; init; }
}