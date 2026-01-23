using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Atoms;

/// <summary>
///     Reticle component - the universal focus/selection indicator.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>R0 Center dot: current focus point</description>
///         </item>
///         <item>
///             <description>R1 Ring: encircles target</description>
///         </item>
///         <item>
///             <description>R2 Snap ticks: cardinal alignment cues</description>
///         </item>
///         <item>
///             <description>R3 Label arc: optional text label</description>
///         </item>
///     </list>
/// </remarks>
public partial class Reticle : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the reticle mode.</summary>
    [Parameter]
    public string Mode { get; set; } = RefractionReticleModes.Focus;

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;
}