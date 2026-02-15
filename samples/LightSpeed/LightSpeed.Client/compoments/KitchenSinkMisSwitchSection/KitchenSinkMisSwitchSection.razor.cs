using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisSwitch demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisSwitchSection : StoreComponent
{
    private const string EventsSectionKey = "misSwitch";

    private static readonly IReadOnlyList<MisSelectOptionViewModel> SwitchStateOptions =
        Enum.GetValues<MisSwitchState>().Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString())).ToList();

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state =>
            KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private IReadOnlyList<string> SwitchEvents =>
        Select<MisSwitchKitchenSinkState, IReadOnlyList<string>>(MisSwitchKitchenSinkSelectors.GetEventLog);

    private MisSwitchViewModel SwitchModel =>
        Select<MisSwitchKitchenSinkState, MisSwitchViewModel>(MisSwitchKitchenSinkSelectors.GetViewModel);

    private bool IsValueToggleChecked =>
        !bool.TryParse(SwitchModel.Value, out bool parsedValue) || parsedValue;

    private static string FormatAction(
        IMisSwitchAction action
    ) =>
        action switch
        {
            MisSwitchInputAction input => $"intent={input.IntentId}, checked={input.IsChecked}",
            MisSwitchChangedAction changed => $"intent={changed.IntentId}, checked={changed.IsChecked}",
            MisSwitchKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisSwitchKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisSwitchPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisSwitchPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisSwitchFocusedAction focused => $"intent={focused.IntentId}",
            MisSwitchBlurredAction blurred => $"intent={blurred.IntentId}",
            var _ => $"intent={action.IntentId}",
        };

    private void HandleClearSwitchEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisSwitchEventsAction());
    }

    private Task HandleMisSwitchActionAsync(
        IMisSwitchAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisSwitchInputAction inputAction:
                Dispatch(new SetMisSwitchCheckedAction(inputAction.IsChecked));
                break;
            case MisSwitchChangedAction changedAction:
                Dispatch(new SetMisSwitchCheckedAction(changedAction.IsChecked));
                break;
        }

        Dispatch(new RecordMisSwitchEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction changedAction &&
            (changedAction.IntentId == "prop-state") &&
            Enum.TryParse(changedAction.Value, true, out MisSwitchState state))
        {
            Dispatch(new SetMisSwitchStateAction(state));
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
            case "prop-value":
                Dispatch(new SetMisSwitchValueAction(checkedValue ? "true" : "false"));
                break;
            case "prop-checked":
                Dispatch(new SetMisSwitchCheckedAction(checkedValue));
                break;
            case "prop-disabled":
                Dispatch(new SetMisSwitchDisabledAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisSwitchRequiredAction(checkedValue));
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
                case "prop-intentid":
                    Dispatch(new SetMisSwitchIntentIdAction(value ?? "kitchen-sink.mis-switch"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisSwitchAriaLabelAction(value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisSwitchTitleAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisSwitchCssClassAction(value));
                    break;
            }
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}