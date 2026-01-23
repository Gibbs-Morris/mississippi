using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Organisms;

/// <summary>
///     Pane component - the primary content surface (Glass Plane).
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>G0 Glass surface: frosted translucent background</description>
///         </item>
///         <item>
///             <description>G1 Header bar: title + meta strip</description>
///         </item>
///         <item>
///             <description>G2 Content well: scrollable interior</description>
///         </item>
///         <item>
///             <description>G3 Footer slot: optional action row</description>
///         </item>
///         <item>
///             <description>G4 Edge glow: focus indicator</description>
///         </item>
///     </list>
/// </remarks>
public partial class Pane : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the main content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the depth band.</summary>
    [Parameter]
    public string Depth { get; set; } = RefractionDepthBands.Mid;

    /// <summary>Gets or sets the footer content.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;

    /// <summary>Gets or sets the pane title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Gets or sets the pane variant.</summary>
    [Parameter]
    public string Variant { get; set; } = RefractionPaneVariants.Primary;
}