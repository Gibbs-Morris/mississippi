using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Client.Components.Molecules.Notifications;

/// <summary>
///     A status message with optional expansion and dismissal actions.
/// </summary>
/// <remarks>
///     <para>Public so applications outside this assembly can compose notification status and actions in Razor markup.</para>
///     <para>
///         Status content remains in a live region. Native buttons report independent intents
///         through callbacks; the parent owns state, detail content, and dismissal.
///     </para>
/// </remarks>
public sealed partial class NotificationPulse : ComponentBase
{
    /// <summary>Gets or sets additional wrapper HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets content inside the status region.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets additional wrapper CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets the ID of the parent-controlled details region.</summary>
    /// <remarks>Must be nonblank when supplied with an expansion callback.</remarks>
    [Parameter]
    public string? DetailsId { get; set; }

    /// <summary>Gets or sets visible dismissal action text.</summary>
    [Parameter]
    public string DismissText { get; set; } = "Dismiss notification";

    /// <summary>Gets or sets visible expansion action text.</summary>
    [Parameter]
    public string ExpandText { get; set; } = "View details";

    /// <summary>Gets or sets the optional parent-controlled expanded state.</summary>
    /// <remarks>When supplied with <see cref="OnExpand" />, renders the current disclosure state.</remarks>
    [Parameter]
    public bool? IsExpanded { get; set; }

    /// <summary>Gets or sets the dismissal intent callback.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the one-way expansion intent callback.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnExpand { get; set; }

    /// <summary>Gets or sets the visual state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.New;

    private string? CallerClass =>
        AdditionalAttributes
            ?.Where(attribute => string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
            .Select(attribute => attribute.Value is bool ? null : attribute.Value?.ToString())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private string CssClass =>
        string.Join(
            " ",
            new[] { "rf-notification-pulse", Class, CallerClass }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string? ExpandAriaExpanded
    {
        get
        {
            bool? isExpanded = IsExpanded;
            if (!isExpanded.HasValue)
            {
                return null;
            }

            return isExpanded.Value ? "true" : "false";
        }
    }

    private IReadOnlyDictionary<string, object>? ForwardedAttributes =>
        AdditionalAttributes
            ?.Where(attribute => !string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase) &&
                                 !string.Equals(attribute.Key, "data-state", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (OnExpand.HasDelegate && string.IsNullOrWhiteSpace(ExpandText))
        {
            throw new ArgumentException("ExpandText must be nonblank when OnExpand is supplied.");
        }

        if (OnExpand.HasDelegate && DetailsId is not null && string.IsNullOrWhiteSpace(DetailsId))
        {
            throw new ArgumentException(
                "DetailsId must be nonblank when OnExpand is supplied and DetailsId is provided.");
        }

        if (OnDismiss.HasDelegate && string.IsNullOrWhiteSpace(DismissText))
        {
            throw new ArgumentException("DismissText must be nonblank when OnDismiss is supplied.");
        }
    }

    private Task HandleDismissAsync() => OnDismiss.InvokeAsync();

    private Task HandleExpandAsync(
        MouseEventArgs e
    ) =>
        OnExpand.InvokeAsync(e);
}