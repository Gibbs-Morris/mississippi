using System;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Components;

/// <summary>
///     Dispatches navigation location updates to the Reservoir store.
/// </summary>
public sealed partial class ReservoirNavigationProvider
    : ComponentBase,
      IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IStore Store { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
        Store.Dispatch(new LocationChangedAction(NavigationManager.Uri, false));
    }

    private void OnLocationChanged(
        object? sender,
        LocationChangedEventArgs e
    ) =>
        Store.Dispatch(new LocationChangedAction(e.Location, e.IsNavigationIntercepted));
}