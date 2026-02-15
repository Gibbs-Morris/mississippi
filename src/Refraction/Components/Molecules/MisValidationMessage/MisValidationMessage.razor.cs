using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a validation message element for form fields.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisValidationMessage : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the content to render as the message.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the validation message view model.
    /// </summary>
    [Parameter]
    public MisValidationMessageViewModel Model { get; set; } = MisValidationMessageViewModel.Default;

    private string AriaLive => Model.Severity == MisValidationMessageSeverity.Error ? "assertive" : "polite";

    private string CssClass
    {
        get
        {
            string severityClass = Model.Severity switch
            {
                MisValidationMessageSeverity.Error => "mis-validation-message--error",
                MisValidationMessageSeverity.Warning => "mis-validation-message--warning",
                MisValidationMessageSeverity.Info => "mis-validation-message--info",
                MisValidationMessageSeverity.Success => "mis-validation-message--success",
                var _ => string.Empty,
            };
            return string.IsNullOrWhiteSpace(Model.CssClass)
                ? $"mis-validation-message {severityClass}"
                : $"mis-validation-message {severityClass} {Model.CssClass}";
        }
    }

    private string Role => Model.Severity == MisValidationMessageSeverity.Error ? "alert" : "status";
}