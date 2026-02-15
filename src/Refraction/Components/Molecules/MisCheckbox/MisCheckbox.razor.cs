using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisCheckboxActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a reusable checkbox component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisCheckbox : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root checkbox element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all checkbox user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisCheckboxAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the checkbox view model.
    /// </summary>
    [Parameter]
    public MisCheckboxViewModel Model { get; set; } = MisCheckboxViewModel.Default;

    private string CssClass
    {
        get
        {
            string stateClass = Model.State switch
            {
                MisCheckboxState.Default => string.Empty,
                MisCheckboxState.Success => "mis-checkbox--success",
                MisCheckboxState.Warning => "mis-checkbox--warning",
                MisCheckboxState.Error => "mis-checkbox--error",
                _ => string.Empty,
            };

            string className = "mis-checkbox";
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

    private async Task OnCheckedChangedAsync(
        bool isChecked
    )
    {
        // Dispatch both input and changed actions for checkbox toggle
        // (checkboxes don't have the same input/change distinction as text inputs)
        await DispatchActionAsync(new MisCheckboxInputAction(Model.IntentId, isChecked));
        await DispatchActionAsync(new MisCheckboxChangedAction(Model.IntentId, isChecked));
    }

    private Task DispatchActionAsync(
        IMisCheckboxAction action
    ) =>
        OnAction.InvokeAsync(action);
}
