using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Rendering;

using Mississippi.Refraction.Client.StateManagement.Scenes;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Concrete test component used to exercise <see cref="ReservoirSceneBase{TState}" /> behavior.
/// </summary>
internal sealed class TestReservoirSceneComponent : ReservoirSceneBase<TestReservoirFeatureState>
{
    /// <summary>
    ///     Creates a dispatch handler for the parameterless helper overload.
    /// </summary>
    /// <param name="actionFactory">The action factory.</param>
    /// <returns>The dispatch handler.</returns>
    public Func<Task> CreateDispatchHandler(
        Func<TestReservoirSceneAction> actionFactory
    ) =>
        DispatchOnEvent(actionFactory);

    /// <summary>
    ///     Creates a dispatch handler for the event-args helper overload.
    /// </summary>
    /// <param name="actionFactory">The action factory.</param>
    /// <returns>The dispatch handler.</returns>
    public Func<string, Task> CreateDispatchHandler(
        Func<string, TestReservoirSceneAction> actionFactory
    ) =>
        DispatchOnEvent<string, TestReservoirSceneAction>(actionFactory);

    /// <summary>
    ///     Reads the current feature state.
    /// </summary>
    /// <returns>The current feature state.</returns>
    public TestReservoirFeatureState ReadState() => State;

    /// <summary>
    ///     Reads the current error flag.
    /// </summary>
    /// <returns>The current error flag.</returns>
    public bool ReadHasError() => HasError;

    /// <summary>
    ///     Reads the current loading flag.
    /// </summary>
    /// <returns>The current loading flag.</returns>
    public bool ReadIsLoading() => IsLoading;

    /// <summary>
    ///     Re-runs initialization to validate subscription replacement behavior.
    /// </summary>
    public void Reinitialize() => OnInitialized();

    /// <inheritdoc />
    protected override void BuildRenderTree(
        RenderTreeBuilder builder
    )
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "test scene");
        builder.CloseElement();
    }
}
