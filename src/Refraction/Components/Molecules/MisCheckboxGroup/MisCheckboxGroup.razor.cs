using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a group of checkboxes for multi-selection.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisCheckboxGroup : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root group element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the checkbox group view model.
    /// </summary>
    [Parameter]
    public MisCheckboxGroupViewModel Model { get; set; } = MisCheckboxGroupViewModel.Default;

    /// <summary>
    ///     Gets or sets the interaction callback for all checkbox group user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisCheckboxGroupAction> OnAction { get; set; }

    private string CheckboxName => Model.Name ?? Model.Id ?? Model.IntentId;

    private string GroupCssClass
    {
        get
        {
            List<string> classes = ["mis-checkbox-group"];

            string stateClass = Model.State switch
            {
                MisCheckboxGroupState.Error => "mis-checkbox-group--error",
                MisCheckboxGroupState.Warning => "mis-checkbox-group--warning",
                MisCheckboxGroupState.Success => "mis-checkbox-group--success",
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                classes.Add(stateClass);
            }

            if (Model.IsDisabled)
            {
                classes.Add("mis-checkbox-group--disabled");
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                classes.Add(Model.CssClass);
            }

            return string.Join(" ", classes);
        }
    }

    private Task OnOptionCheckedChangedAsync(
        string value,
        bool isChecked
    )
    {
        HashSet<string> newValues = new HashSet<string>(Model.Values);

        if (isChecked)
        {
            newValues.Add(value);
        }
        else
        {
            newValues.Remove(value);
        }

        return DispatchActionAsync(new MisCheckboxGroupChangedAction(Model.IntentId, value, isChecked, newValues));
    }

    private Task DispatchActionAsync(
        IMisCheckboxGroupAction action
    ) =>
        OnAction.InvokeAsync(action);
}
