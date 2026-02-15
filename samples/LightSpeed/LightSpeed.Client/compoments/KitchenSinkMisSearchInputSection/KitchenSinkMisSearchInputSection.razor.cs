using System.Collections.Generic;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSearchInputActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisSearchInput demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisSearchInputSection : StoreComponent
{
    private const string EventsSectionKey = "misSearchInput";

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state =>
            KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private IReadOnlyList<string> SearchEvents =>
        Select<MisSearchInputKitchenSinkState, IReadOnlyList<string>>(MisSearchInputKitchenSinkSelectors.GetEventLog);

    private MisSearchInputViewModel SearchModel =>
        Select<MisSearchInputKitchenSinkState, MisSearchInputViewModel>(
            MisSearchInputKitchenSinkSelectors.GetViewModel);

    private static string FormatAction(
        IMisSearchInputAction action
    ) =>
        action switch
        {
            MisSearchInputInputAction input => $"intent={input.IntentId}, value={input.Value}",
            MisSearchInputChangedAction changed => $"intent={changed.IntentId}, value={changed.Value}",
            MisSearchInputClearedAction cleared => $"intent={cleared.IntentId}",
            MisSearchInputFocusedAction focused => $"intent={focused.IntentId}",
            MisSearchInputBlurredAction blurred => $"intent={blurred.IntentId}",
            MisSearchInputSubmittedAction submitted => $"intent={submitted.IntentId}, value={submitted.Value}",
            var _ => action.ToString() ?? string.Empty,
        };

    private void HandleClearEvents()
    {
        Dispatch(new ClearMisSearchInputEventsAction());
    }

    private Task HandleMisSearchInputActionAsync(
        IMisSearchInputAction action
    )
    {
        Dispatch(new RecordMisSearchInputEventAction(action.GetType().Name, FormatAction(action)));

        // Sync state updates
        switch (action)
        {
            case MisSearchInputInputAction input:
                Dispatch(new SetMisSearchInputValueAction(input.Value));
                break;
            case MisSearchInputChangedAction changed:
                Dispatch(new SetMisSearchInputValueAction(changed.Value));
                break;
            case MisSearchInputClearedAction:
                Dispatch(new SetMisSearchInputValueAction(string.Empty));
                break;
        }

        return Task.CompletedTask;
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
            case "prop-isdisabled":
                Dispatch(new SetMisSearchInputDisabledAction(checkedValue));
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
                case "prop-placeholder":
                    Dispatch(new SetMisSearchInputPlaceholderAction(value));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisSearchInputAriaLabelAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisSearchInputCssClassAction(value));
                    break;
            }
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}