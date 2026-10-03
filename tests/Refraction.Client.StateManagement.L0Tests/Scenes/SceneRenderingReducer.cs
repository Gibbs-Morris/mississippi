using Mississippi.Reservoir.Abstractions;


namespace MississippiTests.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Increments the counter without mutating the previous state.
/// </summary>
internal sealed class SceneRenderingReducer : ActionReducerBase<IncrementSceneCounterAction, SceneRenderingState>
{
    /// <inheritdoc />
    public override SceneRenderingState Reduce(
        SceneRenderingState state,
        IncrementSceneCounterAction action
    ) =>
        state with
        {
            Counter = state.Counter + 1,
        };
}