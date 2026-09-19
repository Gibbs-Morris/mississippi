using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Organisms.Confirmations;

/// <summary>
///     SmokeConfirm component - a presentational confirmation surface with native actions.
/// </summary>
/// <remarks>
///     <para>
///         This component follows the state-down, events-up pattern. The parent supplies the
///         title, consequence, state, and callbacks; this component only renders the surface and
///         reports the selected action.
///     </para>
///     <para>
///         The dialog title is required for an accessible name. A meaningful consequence is
///         rendered as an optional description. Cancel and confirm are native buttons that never
///         submit an enclosing form and are disabled independently when their callbacks are absent.
///     </para>
///     <para>
///         Modal lifecycle behavior such as open state, focus containment, Escape handling, and
///         restoration remains the responsibility of a parent composition.
///     </para>
/// </remarks>
public sealed partial class SmokeConfirm : ComponentBase
{
    private static readonly string[] ControlledAttributeNames =
    {
        "aria-describedby", "aria-labelledby", "class", "data-state", "role",
    };

    /// <summary>Gets or sets additional HTML attributes for the confirmation wrapper.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the cancel button text.</summary>
    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>Gets or sets additional CSS classes for the confirmation wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets the confirm button text.</summary>
    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Gets or sets the optional consequence shown below the title.</summary>
    [Parameter]
    public string? Consequence { get; set; }

    /// <summary>Gets or sets the callback when the cancel action is selected.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Gets or sets the callback when the confirm action is selected.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Gets or sets the current visual state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Latent;

    /// <summary>Gets or sets the required dialog title.</summary>
    [Parameter]
    public string? Title { get; set; }

    private string AdditionalClasses =>
        AdditionalAttributes is null
            ? string.Empty
            : string.Join(
                " ",
                AdditionalAttributes
                    .Where(attribute => string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
                    .Select(attribute => attribute.Value?.ToString())
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

    private string ConsequenceId { get; } = $"rf-smoke-confirm-consequence-{Guid.NewGuid():N}";

    private string CssClass =>
        string.Join(
            " ",
            new[] { "rf-smoke-confirm", Class, AdditionalClasses }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string? DescribedBy
    {
        get
        {
            List<string> descriptions = [];
            if (AdditionalAttributes is not null)
            {
                foreach (KeyValuePair<string, object> attribute in AdditionalAttributes.Where(attribute =>
                             string.Equals(attribute.Key, "aria-describedby", StringComparison.OrdinalIgnoreCase)))
                {
                    if (attribute.Value is not string callerDescription)
                    {
                        continue;
                    }

                    foreach (string reference in callerDescription.Split(
                                     [' ', '\t', '\r', '\n', '\f'],
                                     StringSplitOptions.RemoveEmptyEntries)
                                 .Where(reference => !descriptions.Contains(reference, StringComparer.Ordinal)))
                    {
                        descriptions.Add(reference);
                    }
                }
            }

            if (HasConsequence && !descriptions.Contains(ConsequenceId, StringComparer.Ordinal))
            {
                descriptions.Add(ConsequenceId);
            }

            return descriptions.Count == 0 ? null : string.Join(" ", descriptions);
        }
    }

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

    private bool HasConsequence => !string.IsNullOrWhiteSpace(Consequence);

    private string TitleId { get; } = $"rf-smoke-confirm-title-{Guid.NewGuid():N}";

    private static bool IsControlledAttribute(
        string name
    ) =>
        ControlledAttributeNames.Any(attribute => name.Equals(attribute, StringComparison.OrdinalIgnoreCase));

    private static void ValidateRequiredText(
        string? value,
        string parameterName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must be nonblank.", parameterName);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ValidateRequiredText(Title, nameof(Title));
        ValidateRequiredText(CancelText, nameof(CancelText));
        ValidateRequiredText(ConfirmText, nameof(ConfirmText));
    }

    private Task HandleCancelAsync() => OnCancel.HasDelegate ? OnCancel.InvokeAsync() : Task.CompletedTask;

    private Task HandleConfirmAsync() => OnConfirm.HasDelegate ? OnConfirm.InvokeAsync() : Task.CompletedTask;
}