using System;
using System.Collections.Generic;
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

    private readonly List<FocusRequest> pendingFocusRequests = [];

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

    private string DetailsHeadingId => $"{IdPrefix}-details-heading";

    private ElementReference DetailsRegion { get; set; }

    private string DetailsRegionId => $"{IdPrefix}-details";

    private string IdPrefix { get; } = $"notification-demo-{Guid.NewGuid():N}";

    private EventCallback PulseDismissRequested =>
        DismissRequested.HasDelegate ? EventCallback.Factory.Create(this, HandleDismissAsync) : default;

    private EventCallback<MouseEventArgs> PulseExpandRequested =>
        ExpandRequested.HasDelegate ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleExpandAsync) : default;

    private ElementReference RestoreButton { get; set; }

    private ElementReference SectionHeading { get; set; }

    private string SectionHeadingId => $"{IdPrefix}-heading";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(
        bool firstRender
    )
    {
        ElementReference? focusTarget = null;
        for (int index = pendingFocusRequests.Count - 1; index >= 0; index--)
        {
            FocusRequest request = pendingFocusRequests[index];
            ElementReference? targetElement = request.Target switch
            {
                PendingFocus.Details when IsVisible && IsExpanded => DetailsRegion,
                PendingFocus.Restore when !IsVisible => RestoreRequested.HasDelegate ? RestoreButton : SectionHeading,
                PendingFocus.Heading when IsVisible => SectionHeading,
                var _ => null,
            };
            if (targetElement.HasValue)
            {
                focusTarget ??= targetElement;
                pendingFocusRequests.RemoveAt(index);
            }
            else if (request.CallbackCompleted)
            {
                pendingFocusRequests.RemoveAt(index);
            }
        }

        if (focusTarget.HasValue)
        {
            await focusTarget.Value.FocusAsync();
        }
    }

    private Task HandleDismissAsync() =>
        InvokeFocusRequestAsync(PendingFocus.Restore, () => DismissRequested.InvokeAsync());

    private Task HandleExpandAsync(
        MouseEventArgs mouseEventArgs
    ) =>
        InvokeFocusRequestAsync(PendingFocus.Details, () => ExpandRequested.InvokeAsync(mouseEventArgs));

    private Task HandleRestoreAsync() =>
        InvokeFocusRequestAsync(PendingFocus.Heading, () => RestoreRequested.InvokeAsync());

    private async Task InvokeFocusRequestAsync(
        PendingFocus target,
        Func<Task> callback
    )
    {
        FocusRequest request = new(target);
        pendingFocusRequests.Add(request);
        try
        {
            await callback();
            request.CallbackCompleted = true;
        }
        catch
        {
            pendingFocusRequests.Remove(request);
            throw;
        }
    }

    private sealed class FocusRequest
    {
        public FocusRequest(
            PendingFocus target
        ) =>
            Target = target;

        public bool CallbackCompleted { get; set; }

        public PendingFocus Target { get; }
    }
}