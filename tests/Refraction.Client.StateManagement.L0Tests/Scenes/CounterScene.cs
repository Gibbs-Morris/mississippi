using Microsoft.AspNetCore.Components.Rendering;

using Mississippi.Refraction.Client.StateManagement.Scenes;


namespace MississippiTests.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Renders the current counter through the scene's store subscription.
/// </summary>
internal sealed class CounterScene : SceneBase<SceneRenderingState>
{
    /// <inheritdoc />
    protected override void BuildRenderTree(
        RenderTreeBuilder builder
    )
    {
        builder.OpenElement(0, "output");
        builder.AddContent(1, State.Counter);
        builder.CloseElement();
    }
}