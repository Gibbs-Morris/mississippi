using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Client.Components.Molecules.Notifications;

/// <summary>
///     NotificationPulse component - a non-intrusive status message with optional action intents.
/// </summary>
/// <remarks>
///     <para>
///         This component follows the state-down, events-up pattern. Status content is rendered in a
///         stable live region while optional action buttons report one-way intent to the parent.
///         The component never mutates state internally.
///     </para>
///     <para>
///         The attention dot is decorative. Critical remains a visual state and does not change the
///         status region into an assertive alert or add disclosure semantics.
///     </para>
/// </remarks>
public sealed partial class NotificationPulse : ComponentBase
{
    private static readonly string[] ControlledAttributeNames = { "class", "data-state" };

    /// <summary>Gets or sets additional wrapper HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the child content announced in the status region.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets additional wrapper CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets the visible dismiss action text.</summary>
    /// <remarks>Must be nonblank when <see cref="OnDismiss" /> has a delegate.</remarks>
    [Parameter]
    public string DismissText { get; set; } = "Dismiss notification";

    /// <summary>Gets or sets the visible expansion action text.</summary>
    /// <remarks>Must be nonblank when <see cref="OnExpand" /> has a delegate.</remarks>
    [Parameter]
    public string ExpandText { get; set; } = "View details";

    /// <summary>Gets or sets the callback when notification is dismissed.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the one-way callback when details should be expanded.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnExpand { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.New;

    private string AdditionalClasses
    {
        get
        {
            if (AdditionalAttributes is null)
            {
                return string.Empty;
            }

            return string.Join(
                " ",
                AdditionalAttributes
                    .Where(attribute => string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
                    .Select(attribute => attribute.Value?.ToString())
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    private string CssClass =>
        string.Join(
            " ",
            new[] { "rf-notification-pulse", Class, AdditionalClasses }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));

    private IReadOnlyDictionary<string, object>? ForwardedAttributes
    {
        get
        {
            if (AdditionalAttributes is null)
            {
                return null;
            }

            Dictionary<string, object> attributes = AdditionalAttributes
                .Where(attribute => !IsControlledAttribute(attribute.Key))
                .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
            return attributes.Count == 0 ? null : attributes;
        }
    }

    private static bool IsControlledAttribute(
        string name
    ) =>
        ControlledAttributeNames.Any(attribute => name.Equals(attribute, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (OnExpand.HasDelegate && string.IsNullOrWhiteSpace(ExpandText))
        {
            throw new ArgumentException("ExpandText must be nonblank when OnExpand is supplied.");
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