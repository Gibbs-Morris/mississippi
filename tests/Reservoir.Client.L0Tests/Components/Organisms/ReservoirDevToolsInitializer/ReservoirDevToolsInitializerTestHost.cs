using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;


namespace Mississippi.Reservoir.Client.L0Tests.Components.Organisms.ReservoirDevToolsInitializer;

/// <summary>
///     Hosts the DevTools initializer for renderer-based lifecycle tests.
/// </summary>
internal sealed class ReservoirDevToolsInitializerTestHost : ComponentBase
{
    /// <summary>
    ///     Gets or sets the capture service used by the test host.
    /// </summary>
    [Inject]
    private ReservoirDevToolsInitializerTestHostCapture Capture { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(
        RenderTreeBuilder builder
    )
    {
        builder.OpenComponent<ReservoirDevToolsInitializerComponent>(0);
        builder.AddComponentReferenceCapture(
            1,
            component => Capture.Initializer = (ReservoirDevToolsInitializerComponent)component);
        builder.CloseComponent();
    }
}