using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Client.Components.Organisms.Confirmations;

/// <summary>
///     Render a parent-controlled confirmation surface with form-safe native actions.
/// </summary>
public sealed partial class SmokeConfirm : ComponentBase
{
    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class",
        "role",
        "data-state",
        "aria-labelledby",
        "aria-describedby",
    };

    /// <summary>Gets or sets additional HTML attributes on the dialog wrapper.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the cancel action label.</summary>
    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>Gets or sets additional dialog wrapper classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets the confirm action label.</summary>
    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Gets or sets optional consequence text.</summary>
    [Parameter]
    public string? Consequence { get; set; }

    /// <summary>Gets or sets the parent callback for cancel intent.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Gets or sets the parent callback for confirm intent.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Gets or sets the parent-owned visual state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Latent;

    /// <summary>Gets or sets the required dialog title and accessible name.</summary>
    [Parameter]
    public string? Title { get; set; }

    private string ConsequenceId { get; } = $"rf-smoke-confirm-description-{Guid.NewGuid():N}";

    private string CssClass =>
        string.Join(
            " ",
            new[] { "rf-smoke-confirm", Class, GetAttribute("class") }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));

    private string? DescribedBy
    {
        get
        {
            IEnumerable<string> callerIds = (GetAttribute("aria-describedby") ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<string> ids = string.IsNullOrWhiteSpace(Consequence)
                ? callerIds
                : callerIds.Append(ConsequenceId);
            string value = string.Join(" ", ids.Distinct(StringComparer.Ordinal));
            return value.Length == 0 ? null : value;
        }
    }

    private IReadOnlyDictionary<string, object>? ForwardedAttributes =>
        AdditionalAttributes?.Where(pair => !OwnedAttributes.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private string TitleId { get; } = $"rf-smoke-confirm-title-{Guid.NewGuid():N}";

    private static void ValidateLabel(
        string? value,
        string parameterName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A visible label is required.", parameterName);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ValidateLabel(Title, nameof(Title));
        ValidateLabel(CancelText, nameof(CancelText));
        ValidateLabel(ConfirmText, nameof(ConfirmText));
    }

    private string? GetAttribute(
        string name
    )
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        return AdditionalAttributes
            .FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();
    }
}