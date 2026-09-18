using System;

using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client;

/// <summary>
///     Main layout shell for the Spring application.
/// </summary>
/// <remarks>
///     <para>
///         Requests SignalR connection eagerly when the app loads, enabling
///         real-time projection updates across all pages.
///     </para>
/// </remarks>
public sealed partial class MainLayout
    : LayoutComponentBase,
      IDisposable
{
    private IDisposable? storeSubscription;

    /// <summary>
    ///     Gets or sets the inlet store for dispatching actions.
    /// </summary>
    [Inject]
    private IInletStore Store { get; set; } = default!;

    private RefractionThemeMode ThemeMode =>
        Store.Select<ThemePreferencesState, RefractionThemeMode>(ThemePreferencesSelectors.GetThemeMode);

    /// <inheritdoc />
    public void Dispose()
    {
        storeSubscription?.Dispose();
        storeSubscription = null;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        storeSubscription?.Dispose();
        storeSubscription = Store.Subscribe(OnStoreChanged);

        // Request SignalR connection eagerly when the app loads
        Store.Dispatch(new RequestSignalRConnectionAction());
    }

    private void ChangeTheme(
        RefractionThemeMode mode
    ) =>
        Store.Dispatch(new SetThemeModeAction(mode));

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);
}