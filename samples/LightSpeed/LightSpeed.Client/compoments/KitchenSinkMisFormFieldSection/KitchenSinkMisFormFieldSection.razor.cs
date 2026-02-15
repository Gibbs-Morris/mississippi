using System;
using System.Collections.Generic;
using System.Linq;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;

namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisFormField demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisFormFieldSection : StoreComponent
{
    private static readonly IReadOnlyList<MisSelectOptionViewModel> FormFieldStateOptions =
        Enum.GetValues<MisFormFieldState>()
            .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
            .ToList();

    private MisFormFieldViewModel FormFieldModel =>
        Select<MisFormFieldKitchenSinkState, MisFormFieldViewModel>(MisFormFieldKitchenSinkSelectors.GetViewModel);

    private string LabelText =>
        Select<MisFormFieldKitchenSinkState, string>(MisFormFieldKitchenSinkSelectors.GetLabelText);

    private string HelpText =>
        Select<MisFormFieldKitchenSinkState, string>(MisFormFieldKitchenSinkSelectors.GetHelpText);

    private string ValidationMessage =>
        Select<MisFormFieldKitchenSinkState, string>(MisFormFieldKitchenSinkSelectors.GetValidationMessage);

    private string InputValue =>
        Select<MisFormFieldKitchenSinkState, string>(MisFormFieldKitchenSinkSelectors.GetInputValue);

    private bool ShowValidation =>
        Select<MisFormFieldKitchenSinkState, bool>(MisFormFieldKitchenSinkSelectors.GetShowValidation);

    private void HandleInputAction(
        IMisTextInputAction action
    )
    {
        if (action is MisTextInputInputAction inputAction)
        {
            Dispatch(new SetMisFormFieldInputValueAction(inputAction.Value));
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
                case "prop-labeltext":
                    Dispatch(new SetMisFormFieldLabelTextAction(value));
                    break;
                case "prop-helptext":
                    Dispatch(new SetMisFormFieldHelpTextAction(value));
                    break;
                case "prop-validationmsg":
                    Dispatch(new SetMisFormFieldValidationMessageAction(value));
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
            _ => null,
        };

        if (isChecked is not bool checkedValue)
        {
            return;
        }

        switch (action.IntentId)
        {
            case "prop-showvalidation":
                Dispatch(new SetMisFormFieldShowValidationAction(checkedValue));
                break;
            case "prop-disabled":
                Dispatch(new SetMisFormFieldDisabledAction(checkedValue));
                break;
        }
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction selectAction)
        {
            switch (selectAction.IntentId)
            {
                case "prop-state":
                    if (Enum.TryParse<MisFormFieldState>(selectAction.Value, out MisFormFieldState state))
                    {
                        Dispatch(new SetMisFormFieldStateAction(state));
                    }

                    break;
            }
        }
    }
}
