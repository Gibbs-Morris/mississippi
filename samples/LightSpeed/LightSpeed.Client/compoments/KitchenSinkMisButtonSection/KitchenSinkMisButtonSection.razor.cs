using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisButtonActions;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisButton demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisButtonSection : StoreComponent
{
    private const string EventsSectionKey = "misButton";

    private static readonly IReadOnlyList<MisSelectOptionViewModel> ButtonTypeOptions = Enum.GetValues<MisButtonType>()
        .Select(t => new MisSelectOptionViewModel(t.ToString(), t.ToString()))
        .ToList();

    private IReadOnlyList<string> ButtonEvents =>
        Select<MisButtonKitchenSinkState, IReadOnlyList<string>>(MisButtonKitchenSinkSelectors.GetEventLog);

    private MisButtonViewModel ButtonModel =>
        Select<MisButtonKitchenSinkState, MisButtonViewModel>(MisButtonKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state =>
            KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisButtonAction action
    ) =>
        action switch
        {
            MisButtonClickedAction clicked =>
                $"intent={clicked.IntentId}, button={clicked.Button}, ctrl={clicked.CtrlKey}, shift={clicked.ShiftKey}, alt={clicked.AltKey}, meta={clicked.MetaKey}",
            MisButtonPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisButtonPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisButtonKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisButtonKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisButtonFocusedAction focused => $"intent={focused.IntentId}",
            MisButtonBlurredAction blurred => $"intent={blurred.IntentId}",
            var _ => $"intent={action.IntentId}",
        };

    private void HandleClearEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisButtonEventsAction());
    }

    private Task HandleMisButtonActionAsync(
        IMisButtonAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatch(new RecordMisButtonEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        if (action is MisSelectChangedAction changedAction &&
            (changedAction.IntentId == "prop-type") &&
            Enum.TryParse(changedAction.Value, true, out MisButtonType buttonType))
        {
            Dispatch(new SetMisButtonTypeAction(buttonType));
        }
    }

    private void HandlePropertySwitchAction(
        IMisSwitchAction action
    )
    {
        if (action is MisSwitchChangedAction changedAction && (changedAction.IntentId == "prop-disabled"))
        {
            Dispatch(new SetMisButtonDisabledAction(changedAction.IsChecked));
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
                    Dispatch(new SetMisButtonIntentIdAction(value ?? "kitchen-sink.mis-button"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisButtonAriaLabelAction(value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisButtonTitleAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisButtonCssClassAction(value));
                    break;
            }
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}