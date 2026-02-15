using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a reusable radio group component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisRadioGroup : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root radio group element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all radio group user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisRadioGroupAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the radio group view model.
    /// </summary>
    [Parameter]
    public MisRadioGroupViewModel Model { get; set; } = MisRadioGroupViewModel.Default;

    private string GroupCssClass
    {
        get
        {
            string stateClass = Model.State switch
            {
                MisRadioGroupState.Default => string.Empty,
                MisRadioGroupState.Success => "mis-radio-group--success",
                MisRadioGroupState.Warning => "mis-radio-group--warning",
                MisRadioGroupState.Error => "mis-radio-group--error",
                _ => string.Empty,
            };

            string className = "mis-radio-group";
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

    private string RadioName =>
        string.IsNullOrWhiteSpace(Model.Name)
            ? $"mis-radio-group-{Model.IntentId}"
            : Model.Name;

    private bool IsSelected(
        string optionValue
    ) =>
        string.Equals(Model.Value, optionValue, System.StringComparison.Ordinal);

    private async Task OnOptionCheckedChangedAsync(
        string optionValue,
        bool isChecked
    )
    {
        if (!isChecked)
        {
            return;
        }

        string nextValue = optionValue;
        await DispatchActionAsync(new MisRadioGroupInputAction(Model.IntentId, nextValue));
        await DispatchActionAsync(new MisRadioGroupChangedAction(Model.IntentId, nextValue));
    }

    private Task DispatchActionAsync(
        IMisRadioGroupAction action
    ) =>
        OnAction.InvokeAsync(action);
}
