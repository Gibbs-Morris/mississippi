using System;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;


namespace Mississippi.Reservoir.Client.BuiltIn.Components;

/// <summary>
///     Dispatches navigation changes to the Reservoir store.
/// </summary>
public sealed partial class ReservoirNavigationProvider
    : ComponentBase,
      IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirNavigationProvider" /> class.
    /// </summary>
    /// <param name="navigationManager">The Blazor navigation manager.</param>
    /// <param name="store">The Reservoir store.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="navigationManager" /> or <paramref name="store" /> is null.
    /// </exception>
    public ReservoirNavigationProvider(
        NavigationManager navigationManager,
        IStore store
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(store);
        NavigationManager = navigationManager;
        Store = store;
    }

    private NavigationManager NavigationManager { get; }

    private IStore Store { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Subscribe to location changes
        NavigationManager.LocationChanged += OnLocationChanged;

        // Dispatch initial location
        Store.Dispatch(new LocationChangedAction(NavigationManager.Uri, false));
    }

    private void OnLocationChanged(
        object? sender,
        LocationChangedEventArgs e
    ) =>
        Store.Dispatch(new LocationChangedAction(e.Location, e.IsNavigationIntercepted));
}