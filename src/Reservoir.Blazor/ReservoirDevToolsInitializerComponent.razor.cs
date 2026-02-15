using System;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Initializes and shuts down Redux devtools integration for the current Blazor session.
/// </summary>
public sealed partial class ReservoirDevToolsInitializerComponent
    : ComponentBase,
      IDisposable
{
    [Inject]
    private ReduxDevToolsService DevToolsService { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() => DevToolsService.Stop();

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        if (firstRender)
        {
            DevToolsService.Initialize();
        }
    }
}