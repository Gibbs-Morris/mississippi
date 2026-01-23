using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Organisms;

/// <summary>
///     SchematicViewer component - the primary representation for complex objects.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>S0 Viewport: zoom/pan surface</description>
///         </item>
///         <item>
///             <description>S1 Grid/reticle: very faint, not always on</description>
///         </item>
///         <item>
///             <description>S2 Entities: linework layers</description>
///         </item>
///         <item>
///             <description>S3 Selection overlay: reticle + highlight ring</description>
///         </item>
///         <item>
///             <description>S4 Callouts: anchored labels</description>
///         </item>
///         <item>
///             <description>S5 Depth layers: near/mid/far information staging</description>
///         </item>
///     </list>
/// </remarks>
public partial class SchematicViewer : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the caption text.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>Gets or sets the viewport content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;
}