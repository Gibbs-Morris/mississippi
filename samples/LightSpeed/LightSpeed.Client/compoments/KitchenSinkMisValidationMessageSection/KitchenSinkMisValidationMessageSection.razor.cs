using System;
using System.Collections.Generic;
using System.Linq;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;

namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisValidationMessage demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisValidationMessageSection : StoreComponent
{
    private static readonly IReadOnlyList<MisSelectOptionViewModel> SeverityOptions =
        Enum.GetValues<MisValidationMessageSeverity>()
            .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
            .ToList();

    private MisValidationMessageViewModel ValidationModel =>
        Select<MisValidationMessageKitchenSinkState, MisValidationMessageViewModel>(MisValidationMessageKitchenSinkSelectors.GetViewModel);

    private string MessageText =>
        Select<MisValidationMessageKitchenSinkState, string>(MisValidationMessageKitchenSinkSelectors.GetMessageText);

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
                    Dispatch(new SetMisValidationMessageTextAction(value));
                    break;
                case "prop-for":
                    Dispatch(new SetMisValidationMessageForAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisValidationMessageCssClassAction(value));
                    break;
            }
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
                case "prop-severity":
                    if (Enum.TryParse<MisValidationMessageSeverity>(selectAction.Value, out MisValidationMessageSeverity severity))
                    {
                        Dispatch(new SetMisValidationMessageSeverityAction(severity));
                    }

                    break;
            }
        }
    }
}
