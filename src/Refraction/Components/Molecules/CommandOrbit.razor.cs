using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     CommandOrbit component - radial action menu around an emitter.
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
///             <description>O0 Center anchor: origin point (reticle/emitter)</description>
///         </item>
///         <item>
///             <description>O1 Orbit ring: circular path for actions</description>
///         </item>
///         <item>
///             <description>O2 Action nodes: 2-8 action items on ring</description>
///         </item>
///         <item>
///             <description>O3 Sector highlight: indicates current selection</description>
///         </item>
///         <item>
///             <description>O4 Tooltip arc: text near selected node</description>
///         </item>
///     </list>
/// </remarks>
public partial class CommandOrbit : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the child content (action items).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the callback when an action is selected.</summary>
    [Parameter]
    public EventCallback<string> OnActionSelected { get; set; }

    /// <summary>Gets or sets the callback when orbit is dismissed.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Latent;
}