using System;
using System.Collections.Generic;
using System.Linq;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisLabel demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisLabelSection : StoreComponent
{
    private static readonly IReadOnlyList<MisSelectOptionViewModel> LabelStateOptions = Enum.GetValues<MisLabelState>()
        .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
        .ToList();

    private MisLabelViewModel LabelModel =>
        Select<MisLabelKitchenSinkState, MisLabelViewModel>(MisLabelKitchenSinkSelectors.GetViewModel);

    private string LabelText => Select<MisLabelKitchenSinkState, string>(MisLabelKitchenSinkSelectors.GetLabelText);

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction selectAction)
        {
            switch (selectAction.IntentId)
            {
                case "prop-state":
                    if (Enum.TryParse(selectAction.Value, out MisLabelState state))
                    {
                        Dispatch(new SetMisLabelStateAction(state));
                    }

                    break;
            }
        }
    }

    private void HandlePropertySwitchAction(
        IMisSwitchAction action
    )
    {
        bool? isChecked = action switch
        {
            MisSwitchInputAction inputAction => inputAction.IsChecked,
            MisSwitchChangedAction changedAction => changedAction.IsChecked,
            var _ => null,
        };
        if (isChecked is not bool checkedValue)
        {
            return;
        }

        switch (action.IntentId)
        {
            case "prop-isrequired":
                Dispatch(new SetMisLabelIsRequiredAction(checkedValue));
                break;
        }
    }

    private void HandlePropertyTextInputAction(
        IMisTextInputAction action
    )
    {
        if (action is MisTextInputInputAction inputAction)
        {
            string? value = string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value.Trim();
            switch (inputAction.IntentId)
            {
                case "prop-text":
                    Dispatch(new SetMisLabelTextAction(value));
                    break;
                case "prop-for":
                    Dispatch(new SetMisLabelForAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisLabelCssClassAction(value));
                    break;
            }
        }
    }
}