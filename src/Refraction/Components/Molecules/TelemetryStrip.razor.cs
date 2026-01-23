using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     TelemetryStrip component - read-only status display strip.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>T0 Strip container: horizontal bar</description>
///         </item>
///         <item>
///             <description>T1 Segment slots: multiple cells (label + value pairs)</description>
///         </item>
///         <item>
///             <description>T2 Dividers: optional separator marks</description>
///         </item>
///     </list>
/// </remarks>
public partial class TelemetryStrip : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Quiet;
}