using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;

namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisTextInput demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisTextInputSection : StoreComponent
{
    private const string EventsSectionKey = "misTextInput";

    private static IReadOnlyList<MisSelectOptionViewModel> TextInputTypeOptions { get; } =
        Enum.GetValues<MisTextInputType>().Select(t => new MisSelectOptionViewModel(t.ToString(), t.ToString())).ToList();

    private IReadOnlyList<string> TextInputEvents =>
        Select<MisTextInputKitchenSinkState, IReadOnlyList<string>>(MisTextInputKitchenSinkSelectors.GetEventLog);

    private MisTextInputViewModel TextInputModel =>
        Select<MisTextInputKitchenSinkState, MisTextInputViewModel>(MisTextInputKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state => KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisTextInputAction action
    ) =>
        action switch
        {
            MisTextInputInputAction input =>
                $"intent={input.IntentId}, value={input.Value}",
            MisTextInputChangedAction changed =>
                $"intent={changed.IntentId}, value={changed.Value}",
            MisTextInputKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisTextInputKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisTextInputPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisTextInputPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisTextInputFocusedAction focused => $"intent={focused.IntentId}",
            MisTextInputBlurredAction blurred => $"intent={blurred.IntentId}",
            _ => $"intent={action.IntentId}",
        };

    private Task HandleMisTextInputActionAsync(
        IMisTextInputAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisTextInputInputAction inputAction:
                Dispatch(new SetMisTextInputValueAction(inputAction.Value));
                break;
            case MisTextInputChangedAction changedAction:
                Dispatch(new SetMisTextInputValueAction(changedAction.Value));
                break;
        }

        Dispatch(new RecordMisTextInputEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

    private void HandlePropertyTextInputAction(
        IMisTextInputAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextInputInputAction inputAction)
        {
            switch (inputAction.IntentId)
            {
                case "prop-value":
                    Dispatch(new SetMisTextInputValueAction(inputAction.Value ?? string.Empty));
                    break;
                case "prop-intentid":
                    Dispatch(new SetMisTextInputIntentIdAction(!string.IsNullOrWhiteSpace(inputAction.Value) ? inputAction.Value : "kitchen-sink.mis-text-input"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisTextInputAriaLabelAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-placeholder":
                    Dispatch(new SetMisTextInputPlaceholderAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisTextInputTitleAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisTextInputCssClassAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-autocomplete":
                    Dispatch(new SetMisTextInputAutoCompleteAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
            }
        }
    }

    private void HandlePropertySwitchAction(
        IMisSwitchAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
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
            case "prop-disabled":
                Dispatch(new SetMisTextInputDisabledAction(checkedValue));
                break;
            case "prop-readonly":
                Dispatch(new SetMisTextInputReadOnlyAction(checkedValue));
                break;
        }
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisSelectChangedAction changedAction
            && changedAction.IntentId == "prop-type"
            && Enum.TryParse(changedAction.Value, true, out MisTextInputType inputType))
        {
            Dispatch(new SetMisTextInputTypeAction(inputType));
        }
    }

    private void HandleClearTextInputEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisTextInputEventsAction());
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}
