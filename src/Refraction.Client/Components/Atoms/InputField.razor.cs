using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Components.Atoms;

/// <summary>
///     InputField component - instrument-style input with HUD aesthetics.
/// </summary>
/// <remarks>
///     <para>
///         This component follows the state-down, events-up pattern. All data is received
///         via parameters and all user interactions are reported via EventCallbacks.
///         The component never mutates state internally.
///     </para>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>I0 Label: upper-left descriptor</description>
///         </item>
///         <item>
///             <description>I1 Field: monospace input area</description>
///         </item>
///         <item>
///             <description>I2 Constraint tick: optional unit or format indicator</description>
///         </item>
///         <item>
///             <description>I3 Error slot: appears below on invalid state</description>
///         </item>
///     </list>
/// </remarks>
public partial class InputField : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the input id.</summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the input is disabled.</summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>Gets or sets a value indicating whether the input is read-only.</summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>Gets or sets the label text.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Gets or sets the callback when input loses focus.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>Gets or sets the callback when input receives focus.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Gets or sets the placeholder text.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    /// <summary>Gets or sets the input type.</summary>
    [Parameter]
    public string Type { get; set; } = "text";

    /// <summary>Gets or sets the current value.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback when value changes.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Handles blur event.</summary>
    private Task HandleBlurAsync(
        FocusEventArgs e
    ) =>
        OnBlur.InvokeAsync(e);

    /// <summary>Handles focus event.</summary>
    private Task HandleFocusAsync(
        FocusEventArgs e
    ) =>
        OnFocus.InvokeAsync(e);

    /// <summary>Handles input change event - reports to parent, does not mutate state.</summary>
    private Task HandleInputAsync(
        ChangeEventArgs e
    )
    {
        string newValue = e.Value?.ToString() ?? string.Empty;
        return ValueChanged.InvokeAsync(newValue);
    }
}