using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a form field wrapper that combines label, control, validation, and help text.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisFormField : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root container element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the main control content (input, select, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the help text content.
    /// </summary>
    [Parameter]
    public RenderFragment? HelpText { get; set; }

    /// <summary>
    ///     Gets or sets the label content.
    /// </summary>
    [Parameter]
    public RenderFragment? Label { get; set; }

    /// <summary>
    ///     Gets or sets the form field view model.
    /// </summary>
    [Parameter]
    public MisFormFieldViewModel Model { get; set; } = MisFormFieldViewModel.Default;

    /// <summary>
    ///     Gets or sets the validation message content.
    /// </summary>
    [Parameter]
    public RenderFragment? Validation { get; set; }

    private string CssClass
    {
        get
        {
            List<string> classes = ["mis-form-field"];

            string stateClass = Model.State switch
            {
                MisFormFieldState.Error => "mis-form-field--error",
                MisFormFieldState.Warning => "mis-form-field--warning",
                MisFormFieldState.Success => "mis-form-field--success",
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                classes.Add(stateClass);
            }

            if (Model.IsDisabled)
            {
                classes.Add("mis-form-field--disabled");
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                classes.Add(Model.CssClass);
            }

            return string.Join(" ", classes);
        }
    }
}
