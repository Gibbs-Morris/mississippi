using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a basic reusable text input component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisTextInput : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root input element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all text input user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisTextInputAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the text input view model.
    /// </summary>
    [Parameter]
    public MisTextInputViewModel Model { get; set; } = MisTextInputViewModel.Default;

    private string CssClass =>
        string.IsNullOrWhiteSpace(Model.CssClass)
            ? "text-input"
            : $"text-input {Model.CssClass}";

    private string InputType =>
        Model.Type switch
        {
            MisTextInputType.Text => "text",
            MisTextInputType.Email => "email",
            MisTextInputType.Password => "password",
            MisTextInputType.Search => "search",
            MisTextInputType.Tel => "tel",
            MisTextInputType.Url => "url",
            _ => "text",
        };

    private Task DispatchActionAsync(
        IMisTextInputAction action
    ) =>
        OnAction.InvokeAsync(action);
}