using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace MississippiSamples.LightSpeed.Client.Components.Organisms.Notifications;

/// <summary>Demonstrates a controlled notification workflow without owning application state.</summary>
/// <remarks>
///     The parent supplies notification state and callbacks. This organism only tracks the next UI
///     focus target while a callback changes the rendered composition.
/// </remarks>
public sealed partial class NotificationDemo : ComponentBase
{
    private enum PendingFocus
    {
        None,

        Details,

        Restore,

        Heading,
    }

    private PendingFocus pendingFocus;

    /// <summary>Gets or sets the dismissal callback.</summary>
    [Parameter]
    public EventCallback DismissRequested { get; set; }

    /// <summary>Gets or sets the expansion callback.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> ExpandRequested { get; set; }

    /// <summary>Gets or sets a value indicating whether the notification details are expanded.</summary>
    [Parameter]
    public bool IsExpanded { get; set; }

    /// <summary>Gets or sets a value indicating whether the notification is visible.</summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>Gets or sets the restoration callback.</summary>
    [Parameter]
    public EventCallback RestoreRequested { get; set; }

    private ElementReference DetailsRegion { get; set; }

    private ElementReference RestoreButton { get; set; }

    private ElementReference SectionHeading { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(
        bool firstRender
    )
    {
        PendingFocus nextFocus = pendingFocus;
        pendingFocus = PendingFocus.None;
        if ((nextFocus == PendingFocus.Details) && IsVisible && IsExpanded)
        {
            await DetailsRegion.FocusAsync().AsTask();
        }
        else if ((nextFocus == PendingFocus.Restore) && !IsVisible)
        {
            await RestoreButton.FocusAsync().AsTask();
        }
        else if ((nextFocus == PendingFocus.Heading) && IsVisible && !IsExpanded)
        {
            await SectionHeading.FocusAsync().AsTask();
        }
    }

    private Task HandleDismissAsync()
    {
        pendingFocus = PendingFocus.Restore;
        return DismissRequested.InvokeAsync();
    }

    private Task HandleExpandAsync(
        MouseEventArgs mouseEventArgs
    )
    {
        pendingFocus = PendingFocus.Details;
        return ExpandRequested.InvokeAsync(mouseEventArgs);
    }

    private Task HandleRestoreAsync()
    {
        pendingFocus = PendingFocus.Heading;
        return RestoreRequested.InvokeAsync();
    }
}