using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;


namespace Spring.Client;

/// <summary>
///     Main layout shell for the Spring application.
/// </summary>
/// <remarks>
///     <para>
///         Requests SignalR connection eagerly when the app loads, enabling
///         real-time projection updates across all pages.
///     </para>
/// </remarks>
public sealed partial class MainLayout : LayoutComponentBase
{
    /// <summary>
    ///     Gets or sets the inlet store for dispatching actions.
    /// </summary>
    [Inject]
    private IInletStore Store { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Request SignalR connection eagerly when the app loads
        Store.Dispatch(new RequestSignalRConnectionAction());
    }
}
