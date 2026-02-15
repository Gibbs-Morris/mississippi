using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisSwitchActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a reusable switch component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisSwitch : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root switch element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all switch user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisSwitchAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the switch view model.
    /// </summary>
    [Parameter]
    public MisSwitchViewModel Model { get; set; } = MisSwitchViewModel.Default;

    private string CssClass
    {
        get
        {
            string stateClass = Model.State switch
            {
                MisSwitchState.Default => string.Empty,
                MisSwitchState.Success => "mis-switch--success",
                MisSwitchState.Warning => "mis-switch--warning",
                MisSwitchState.Error => "mis-switch--error",
                _ => string.Empty,
            };

            string className = "mis-switch";
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
        // Dispatch both input and changed actions for switch toggle
        // (switches don't have the same input/change distinction as text inputs)
        await DispatchActionAsync(new MisSwitchInputAction(Model.IntentId, isChecked));
        await DispatchActionAsync(new MisSwitchChangedAction(Model.IntentId, isChecked));
    }

    private Task DispatchActionAsync(
        IMisSwitchAction action
    ) =>
        OnAction.InvokeAsync(action);
}
