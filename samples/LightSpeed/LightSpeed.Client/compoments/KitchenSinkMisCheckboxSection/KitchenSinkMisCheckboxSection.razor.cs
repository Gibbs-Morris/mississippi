using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisCheckboxActions;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisCheckbox demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisCheckboxSection : StoreComponent
{
    private const string EventsSectionKey = "misCheckbox";

    private static readonly IReadOnlyList<MisSelectOptionViewModel> CheckboxStateOptions =
        Enum.GetValues<MisCheckboxState>()
            .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
            .ToList();

    private IReadOnlyList<string> CheckboxEvents =>
        Select<MisCheckboxKitchenSinkState, IReadOnlyList<string>>(MisCheckboxKitchenSinkSelectors.GetEventLog);

    private MisCheckboxViewModel CheckboxModel =>
        Select<MisCheckboxKitchenSinkState, MisCheckboxViewModel>(MisCheckboxKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state =>
            KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisCheckboxAction action
    ) =>
        action switch
        {
            MisCheckboxInputAction input => $"intent={input.IntentId}, checked={input.IsChecked}",
            MisCheckboxChangedAction changed => $"intent={changed.IntentId}, checked={changed.IsChecked}",
            MisCheckboxKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisCheckboxKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisCheckboxPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisCheckboxPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisCheckboxFocusedAction focused => $"intent={focused.IntentId}",
            MisCheckboxBlurredAction blurred => $"intent={blurred.IntentId}",
            var _ => $"intent={action.IntentId}",
        };

    private void HandleClearCheckboxEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisCheckboxEventsAction());
    }

    private Task HandleMisCheckboxActionAsync(
        IMisCheckboxAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisCheckboxInputAction inputAction:
                Dispatch(new SetMisCheckboxCheckedAction(inputAction.IsChecked));
                break;
            case MisCheckboxChangedAction changedAction:
                Dispatch(new SetMisCheckboxCheckedAction(changedAction.IsChecked));
                break;
        }

        Dispatch(new RecordMisCheckboxEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction changedAction &&
            (changedAction.IntentId == "prop-state") &&
            Enum.TryParse(changedAction.Value, true, out MisCheckboxState state))
        {
            Dispatch(new SetMisCheckboxStateAction(state));
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
            case "prop-checked":
                Dispatch(new SetMisCheckboxCheckedAction(checkedValue));
                break;
            case "prop-disabled":
                Dispatch(new SetMisCheckboxDisabledAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisCheckboxRequiredAction(checkedValue));
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
                case "prop-value":
                    Dispatch(new SetMisCheckboxValueAction(value ?? "true"));
                    break;
                case "prop-intentid":
                    Dispatch(new SetMisCheckboxIntentIdAction(value ?? "kitchen-sink.mis-checkbox"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisCheckboxAriaLabelAction(value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisCheckboxTitleAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisCheckboxCssClassAction(value));
                    break;
            }
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}