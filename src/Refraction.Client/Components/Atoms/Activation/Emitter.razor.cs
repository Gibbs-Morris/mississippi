using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Client.Components.Atoms.Activation;

/// <summary>
///     Emitter component - a persistent origin point for materialize/dematerialize
///     gestures and command reticle invocation.
/// </summary>
/// <remarks>
///     <para>
///         This component follows the state-down, events-up pattern. All data is received
///         via parameters and all user interactions are reported via EventCallbacks.
///         The component never mutates state internally.
///     </para>
///     <para>
///         The public button can expose an optional visible label or remain icon-only when the caller
///         supplies a native accessible name. The decorative seed remains an 8px visual identity.
///     </para>
///     <para>
///         Every rendered emitter must receive a nonblank visible label or a nonblank string
///         <c>aria-label</c>/<c>aria-labelledby</c> parameter. An
///         <see cref="InvalidOperationException" /> is thrown when no valid naming parameter is supplied
///         or a supplied ARIA naming value is not a string.
///     </para>
/// </remarks>
public sealed partial class Emitter : ComponentBase
{
    private static readonly string[] AccessibleNameAttributeNames = { "aria-label", "aria-labelledby" };

    /// <summary>Gets or sets additional native button attributes.</summary>
    /// <remarks>
    ///     Controlled type, disabled, class, state and event attributes take precedence. The
    ///     <c>aria-label</c> and <c>aria-labelledby</c> values must be strings when supplied.
    /// </remarks>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets additional CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets a value indicating whether the emitter is disabled.</summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    ///     Gets or sets the optional visible label. When blank, a nonblank string
    ///     <c>aria-label</c> or <c>aria-labelledby</c> attribute is required.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Gets or sets the callback when emitter is activated (clicked/tapped).</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnActivate { get; set; }

    /// <summary>Gets or sets the callback when emitter receives focus.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    private string AriaDisabled => IsEffectivelyDisabled ? "true" : "false";

    private string CssClass
    {
        get
        {
            string? callerClass = AdditionalAttributes
                ?.FirstOrDefault(attribute => string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
                .Value?.ToString();
            return string.Join(
                " ",
                new[] { "rf-emitter", Class, callerClass }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    private string EffectiveState => IsEffectivelyDisabled ? RefractionStates.Disabled : State;

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
                .Where(attribute => !IsAccessibleNameAttribute(attribute.Key))
                .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
            foreach (string attributeName in AccessibleNameAttributeNames)
            {
                KeyValuePair<string, object>? attribute = GetLastAttribute(attributeName);
                if (attribute.HasValue)
                {
                    attributes[attribute.Value.Key] = attribute.Value.Value;
                }
            }

            return attributes.Count == 0 ? null : attributes;
        }
    }

    private bool IsEffectivelyDisabled => IsDisabled || (State == RefractionStates.Disabled);

    private static bool IsAccessibleNameAttribute(
        string name
    ) =>
        name.Equals("aria-label", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase);

    private static bool IsControlledAttribute(
        string name
    ) =>
        name.Equals("aria-disabled", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("class", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("data-state", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("disabled", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("onclick", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("onfocus", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("type", StringComparison.OrdinalIgnoreCase);

    /// <summary>Validates the supplied accessible naming parameter values.</summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no nonblank naming parameter is supplied or a supplied ARIA naming value is not a string.
    /// </exception>
    /// <inheritdoc />
    protected override void OnParametersSet() => ValidateAccessibleName();

    private KeyValuePair<string, object>? GetLastAttribute(
        string name
    )
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        return AdditionalAttributes
            .Where(attribute => string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase))
            .Select(attribute => (KeyValuePair<string, object>?)attribute)
            .LastOrDefault();
    }

    /// <summary>Handles activation (click) events when enabled.</summary>
    private Task HandleClickAsync(
        MouseEventArgs e
    ) =>
        IsEffectivelyDisabled ? Task.CompletedTask : OnActivate.InvokeAsync(e);

    /// <summary>Handles focus events when enabled.</summary>
    private Task HandleFocusAsync(
        FocusEventArgs e
    ) =>
        IsEffectivelyDisabled ? Task.CompletedTask : OnFocus.InvokeAsync(e);

    private bool HasMeaningfulAccessibleNameAttribute(
        string name
    )
    {
        KeyValuePair<string, object>? attribute = GetLastAttribute(name);
        return attribute.HasValue && attribute.Value.Value is string value && !string.IsNullOrWhiteSpace(value);
    }

    private void ValidateAccessibleName()
    {
        foreach (string attributeName in AccessibleNameAttributeNames)
        {
            KeyValuePair<string, object>? attribute = GetLastAttribute(attributeName);
            if (attribute.HasValue && attribute.Value.Value is not null && attribute.Value.Value is not string)
            {
                throw new InvalidOperationException(
                    $"Emitter {attributeName} must be a string when supplied. Provide a nonblank Label, aria-label, or aria-labelledby.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Label) ||
            HasMeaningfulAccessibleNameAttribute("aria-label") ||
            HasMeaningfulAccessibleNameAttribute("aria-labelledby"))
        {
            return;
        }

        throw new InvalidOperationException(
            "Emitter requires a nonblank Label, aria-label, or aria-labelledby accessible name. " +
            "Icon-only emitters must provide a nonblank string aria-label or aria-labelledby attribute.");
    }
}