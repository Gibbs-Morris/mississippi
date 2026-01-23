using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Atoms;

/// <summary>
///     CalloutLine component - anchors labels to schematic elements.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>C0 Anchor dot: attaches to target element</description>
///         </item>
///         <item>
///             <description>C1 Leader line: angled segment to label</description>
///         </item>
///         <item>
///             <description>C2 Horizontal segment: runs parallel to label</description>
///         </item>
///         <item>
///             <description>C3 Label: text/badge block</description>
///         </item>
///     </list>
/// </remarks>
public partial class CalloutLine : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the label text.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Idle;
}