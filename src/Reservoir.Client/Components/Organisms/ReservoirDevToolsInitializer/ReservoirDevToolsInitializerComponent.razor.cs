using System;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Reservoir.Client.Components.Organisms.ReservoirDevToolsInitializer;

/// <summary>
///     Initializes Reservoir Redux DevTools after the component renders.
/// </summary>
/// <remarks>
///     This component is public so consuming applications can compose it in Razor.
/// </remarks>
public sealed partial class ReservoirDevToolsInitializerComponent
    : ComponentBase,
      IDisposable
{
    /// <summary>
    ///     Gets or sets the Redux DevTools service injected by Blazor.
    /// </summary>
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