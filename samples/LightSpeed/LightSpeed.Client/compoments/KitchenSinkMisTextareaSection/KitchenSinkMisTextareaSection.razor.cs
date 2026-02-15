using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextareaActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;

namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisTextarea demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisTextareaSection : StoreComponent
{
    private const string EventsSectionKey = "misTextarea";

    private static IReadOnlyList<MisSelectOptionViewModel> TextareaStateOptions { get; } =
        Enum.GetValues<MisTextareaState>().Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString())).ToList();

    private IReadOnlyList<string> TextareaEvents =>
        Select<MisTextareaKitchenSinkState, IReadOnlyList<string>>(MisTextareaKitchenSinkSelectors.GetEventLog);

    private MisTextareaViewModel TextareaModel =>
        Select<MisTextareaKitchenSinkState, MisTextareaViewModel>(MisTextareaKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state => KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisTextareaAction action
    ) =>
        action switch
        {
            MisTextareaInputAction input =>
                $"intent={input.IntentId}, value={input.Value}",
            MisTextareaChangedAction changed =>
                $"intent={changed.IntentId}, value={changed.Value}",
            MisTextareaKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisTextareaKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisTextareaPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisTextareaPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisTextareaFocusedAction focused => $"intent={focused.IntentId}",
            MisTextareaBlurredAction blurred => $"intent={blurred.IntentId}",
            _ => $"intent={action.IntentId}",
        };

    private Task HandleMisTextareaActionAsync(
        IMisTextareaAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisTextareaInputAction inputAction:
                Dispatch(new SetMisTextareaValueAction(inputAction.Value));
                break;
            case MisTextareaChangedAction changedAction:
                Dispatch(new SetMisTextareaValueAction(changedAction.Value));
                break;
        }

        Dispatch(new RecordMisTextareaEventAction(action.GetType().Name, FormatAction(action)));
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
                case "prop-intentid":
                    Dispatch(new SetMisTextareaIntentIdAction(!string.IsNullOrWhiteSpace(inputAction.Value) ? inputAction.Value : "kitchen-sink.mis-textarea"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisTextareaAriaLabelAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-placeholder":
                    Dispatch(new SetMisTextareaPlaceholderAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisTextareaTitleAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisTextareaCssClassAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-rows":
                    if (int.TryParse(inputAction.Value, out int rows) && rows > 0)
                    {
                        Dispatch(new SetMisTextareaRowsAction(rows));
                    }

                    break;
            }
        }
    }

    private void HandlePropertyTextareaAction(
        IMisTextareaAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextareaInputAction inputAction && inputAction.IntentId == "prop-value")
        {
            Dispatch(new SetMisTextareaValueAction(inputAction.Value ?? string.Empty));
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
                Dispatch(new SetMisTextareaDisabledAction(checkedValue));
                break;
            case "prop-readonly":
                Dispatch(new SetMisTextareaReadOnlyAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisTextareaRequiredAction(checkedValue));
                break;
        }
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisSelectChangedAction changedAction
            && changedAction.IntentId == "prop-state"
            && Enum.TryParse(changedAction.Value, true, out MisTextareaState state))
        {
            Dispatch(new SetMisTextareaStateAction(state));
        }
    }

    private void HandleClearTextareaEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisTextareaEventsAction());
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}
