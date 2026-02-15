using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a help text element for form fields.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisHelpText : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the content to render as the help text.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the help text view model.
    /// </summary>
    [Parameter]
    public MisHelpTextViewModel Model { get; set; } = MisHelpTextViewModel.Default;

    private string CssClass =>
        string.IsNullOrWhiteSpace(Model.CssClass)
            ? "mis-help-text"
            : $"mis-help-text {Model.CssClass}";
}
