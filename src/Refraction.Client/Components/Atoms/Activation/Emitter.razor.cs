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
/// </remarks>
public sealed partial class Emitter : ComponentBase
{
    /// <summary>Gets or sets additional native button attributes.</summary>
    /// <remarks>Controlled type, disabled, class, state and event attributes take precedence.</remarks>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets additional CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Gets or sets a value indicating whether the emitter is disabled.</summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>Gets or sets the optional visible label. Icon-only callers must provide an accessible name.</summary>
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
                .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
            return attributes.Count == 0 ? null : attributes;
        }
    }

    private bool IsEffectivelyDisabled => IsDisabled || (State == RefractionStates.Disabled);

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
}