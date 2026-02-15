using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a label element for form fields.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisLabel : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root label element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the content to render inside the label.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the label view model.
    /// </summary>
    [Parameter]
    public MisLabelViewModel Model { get; set; } = MisLabelViewModel.Default;

    private string CssClass
    {
        get
        {
            List<string> classes = ["mis-label"];

            string stateClass = Model.State switch
            {
                MisLabelState.Error => "mis-label--error",
                MisLabelState.Warning => "mis-label--warning",
                MisLabelState.Disabled => "mis-label--disabled",
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                classes.Add(stateClass);
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                classes.Add(Model.CssClass);
            }

            return string.Join(" ", classes);
        }
    }
}
