using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisButtonActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a basic reusable button atom for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisButton : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root button element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets optional child content to render inside the button.
    /// </summary>
    /// <remarks>
    ///     Use this for text, icons, badges, counters, or other rich button layouts.
    /// </remarks>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all button user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisButtonAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the button view model.
    /// </summary>
    [Parameter]
    public MisButtonViewModel Model { get; set; } = MisButtonViewModel.Default;

    private string CssClass =>
        string.IsNullOrWhiteSpace(Model.CssClass)
            ? "mis-button"
            : $"mis-button {Model.CssClass}";

    private string ButtonType =>
        Model.Type switch
        {
            MisButtonType.Button => "button",
            MisButtonType.Submit => "submit",
            MisButtonType.Reset => "reset",
            _ => throw new InvalidOperationException($"Unsupported button type: {Model.Type}"),
        };

    private Task DispatchActionAsync(
        IMisButtonAction action
    ) =>
        OnAction.HasDelegate
            ? OnAction.InvokeAsync(action)
            : Task.CompletedTask;
}
