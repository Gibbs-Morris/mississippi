using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisPasswordInput demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisPasswordInputSection : StoreComponent
{
    private const string EventsSectionKey = "misPasswordInput";

    private static readonly IReadOnlyList<MisSelectOptionViewModel> PasswordStateOptions =
        Enum.GetValues<MisPasswordInputState>()
            .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
            .ToList();

    private IReadOnlyList<string> PasswordEvents =>
        Select<MisPasswordInputKitchenSinkState, IReadOnlyList<string>>(MisPasswordInputKitchenSinkSelectors.GetEventLog);

    private MisPasswordInputViewModel PasswordModel =>
        Select<MisPasswordInputKitchenSinkState, MisPasswordInputViewModel>(MisPasswordInputKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(
            state => KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisPasswordInputAction action
    ) =>
        action switch
        {
            MisPasswordInputInputAction input =>
                $"intent={input.IntentId}, value.len={input.Value?.Length ?? 0}",
            MisPasswordInputChangedAction changed =>
                $"intent={changed.IntentId}, value.len={changed.Value?.Length ?? 0}",
            MisPasswordInputToggleVisibilityAction toggle =>
                $"intent={toggle.IntentId}, isVisible={toggle.IsPasswordVisible}",
            MisPasswordInputFocusedAction focused =>
                $"intent={focused.IntentId}",
            MisPasswordInputBlurredAction blurred =>
                $"intent={blurred.IntentId}",
            _ => action.ToString() ?? string.Empty,
        };

    private Task HandleMisPasswordInputActionAsync(
        IMisPasswordInputAction action
    )
    {
        Dispatch(new RecordMisPasswordInputEventAction(action.GetType().Name, FormatAction(action)));

        // Sync state updates
        switch (action)
        {
            case MisPasswordInputInputAction input:
                Dispatch(new SetMisPasswordInputValueAction(input.Value));
                break;
            case MisPasswordInputToggleVisibilityAction toggle:
                Dispatch(new SetMisPasswordInputVisibilityAction(toggle.IsPasswordVisible));
                break;
        }

        return Task.CompletedTask;
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
                case "prop-placeholder":
                    Dispatch(new SetMisPasswordInputPlaceholderAction(value));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisPasswordInputAriaLabelAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisPasswordInputCssClassAction(value));
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
            case "prop-isdisabled":
                Dispatch(new SetMisPasswordInputDisabledAction(checkedValue));
                break;
            case "prop-isreadonly":
                Dispatch(new SetMisPasswordInputReadOnlyAction(checkedValue));
                break;
            case "prop-ispasswordvisible":
                Dispatch(new SetMisPasswordInputVisibilityAction(checkedValue));
                break;
        }
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction changedAction &&
            changedAction.IntentId == "prop-state" &&
            Enum.TryParse<MisPasswordInputState>(changedAction.Value, out MisPasswordInputState state))
        {
            Dispatch(new SetMisPasswordInputStateAction(state));
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }

    private void HandleClearEvents()
    {
        Dispatch(new ClearMisPasswordInputEventsAction());
    }
}
