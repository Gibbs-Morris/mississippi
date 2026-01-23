using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Atoms;

/// <summary>
///     ProgressArc component - an arc-based progress indicator.
/// </summary>
/// <remarks>
///     <para>Anatomy:</para>
///     <list type="bullet">
///         <item>
///             <description>P0 Track: faint background arc (0-360°)</description>
///         </item>
///         <item>
///             <description>P1 Fill arc: animated progress stroke</description>
///         </item>
///         <item>
///             <description>P2 Endpoint: optional glyph at current position</description>
///         </item>
///         <item>
///             <description>P3 Center label: percentage or status text</description>
///         </item>
///     </list>
/// </remarks>
public partial class ProgressArc : ComponentBase
{
    /// <summary>Gets or sets additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the maximum value.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Gets or sets the minimum value.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>Gets or sets the current component state.</summary>
    [Parameter]
    public string State { get; set; } = RefractionStates.Determinate;

    /// <summary>Gets or sets the current value.</summary>
    [Parameter]
    public double Value { get; set; }
}