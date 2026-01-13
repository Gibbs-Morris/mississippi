using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.Badge;

/// <summary>
///     Atomic component for displaying notification badges.
/// </summary>
public sealed partial class Badge : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the badge content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the badge variant. Valid values: primary, success, warning, danger, muted.
    /// </summary>
    [Parameter]
    public string Variant { get; set; } = "primary";

    private string VariantClass =>
        Variant switch
        {
            "success" => "badge--success",
            "warning" => "badge--warning",
            "danger" => "badge--danger",
            "muted" => "badge--muted",
            var _ => "badge--primary",
        };
}