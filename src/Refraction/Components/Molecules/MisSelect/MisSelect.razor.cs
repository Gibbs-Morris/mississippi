using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisSelectActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a reusable select component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisSelect : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root select element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all select user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisSelectAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the select view model.
    /// </summary>
    [Parameter]
    public MisSelectViewModel Model { get; set; } = MisSelectViewModel.Default;

    private bool HasPlaceholder =>
        !string.IsNullOrWhiteSpace(Model.Placeholder);

    private string CssClass
    {
        get
        {
            string stateClass = Model.State switch
            {
                MisSelectState.Default => string.Empty,
                MisSelectState.Success => "mis-select--success",
                MisSelectState.Warning => "mis-select--warning",
                MisSelectState.Error => "mis-select--error",
                _ => string.Empty,
            };

            string className = "mis-select";
            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                className = $"{className} {stateClass}";
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                className = $"{className} {Model.CssClass}";
            }

            return className;
        }
    }

    private Task DispatchActionAsync(
        IMisSelectAction action
    ) =>
        OnAction.InvokeAsync(action);
}