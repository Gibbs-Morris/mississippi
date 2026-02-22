using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace Mississippi.Refraction.Components.Atoms;

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
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>E0 Seed: minimal idle indicator (dot)</description>
///         </item>
///         <item>
///             <description>E1 Ring: expands when armed/busy</description>
///         </item>
///         <item>
///             <description>E2 Pulse: subtle activity indicator</description>
///         </item>
///         <item>
///             <description>E3 Eject line: indicates direction of materialization</description>
///         </item>
///         <item>
///             <description>E4 Mode glyph: indicates palette state or power mode</description>
///         </item>
///     </list>
/// </remarks>
public partial class Emitter : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the callback when emitter is activated (clicked/tapped).</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnActivate { get; set; }

    /// <summary>Gets or sets the callback when emitter receives focus.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    /// <summary>Handles activation (click) event.</summary>
    private Task HandleClickAsync(
        MouseEventArgs e
    ) =>
        OnActivate.InvokeAsync(e);

    /// <summary>Handles focus event.</summary>
    private Task HandleFocusAsync(
        FocusEventArgs e
    ) =>
        OnFocus.InvokeAsync(e);
}