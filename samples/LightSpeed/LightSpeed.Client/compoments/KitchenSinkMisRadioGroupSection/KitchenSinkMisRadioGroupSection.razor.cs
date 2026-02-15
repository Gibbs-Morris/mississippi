using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextareaActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;

namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisRadioGroup demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisRadioGroupSection : StoreComponent
{
    private const string EventsSectionKey = "misRadioGroup";

    private static IReadOnlyList<MisSelectOptionViewModel> RadioGroupStateOptions { get; } =
        Enum.GetValues<MisRadioGroupState>().Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString())).ToList();

    private IReadOnlyList<string> RadioGroupEvents =>
        Select<MisRadioGroupKitchenSinkState, IReadOnlyList<string>>(MisRadioGroupKitchenSinkSelectors.GetEventLog);

    private MisRadioGroupViewModel RadioGroupModel =>
        Select<MisRadioGroupKitchenSinkState, MisRadioGroupViewModel>(MisRadioGroupKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state => KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisRadioGroupAction action
    ) =>
        action switch
        {
            MisRadioGroupInputAction input =>
                $"intent={input.IntentId}, value={input.Value}",
            MisRadioGroupChangedAction changed =>
                $"intent={changed.IntentId}, value={changed.Value}",
            MisRadioGroupKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, option={keyDown.OptionValue}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisRadioGroupKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, option={keyUp.OptionValue}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisRadioGroupPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, option={pointerDown.OptionValue}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisRadioGroupPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, option={pointerUp.OptionValue}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisRadioGroupFocusedAction focused => $"intent={focused.IntentId}, option={focused.OptionValue}",
            MisRadioGroupBlurredAction blurred => $"intent={blurred.IntentId}, option={blurred.OptionValue}",
            _ => $"intent={action.IntentId}",
        };

    private Task HandleMisRadioGroupActionAsync(
        IMisRadioGroupAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisRadioGroupInputAction inputAction:
                Dispatch(new SetMisRadioGroupValueAction(inputAction.Value));
                break;
            case MisRadioGroupChangedAction changedAction:
                Dispatch(new SetMisRadioGroupValueAction(changedAction.Value));
                break;
        }

        Dispatch(new RecordMisRadioGroupEventAction(action.GetType().Name, FormatAction(action)));
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
                    Dispatch(new SetMisRadioGroupValueAction(inputAction.Value ?? string.Empty));
                    break;
                case "prop-intentid":
                    Dispatch(new SetMisRadioGroupIntentIdAction(!string.IsNullOrWhiteSpace(inputAction.Value) ? inputAction.Value : "kitchen-sink.mis-radio-group"));
                    break;
                case "prop-arialabel":
                    Dispatch(new SetMisRadioGroupAriaLabelAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-title":
                    Dispatch(new SetMisRadioGroupTitleAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisRadioGroupCssClassAction(string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
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
                Dispatch(new SetMisRadioGroupDisabledAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisRadioGroupRequiredAction(checkedValue));
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
            && Enum.TryParse(changedAction.Value, true, out MisRadioGroupState state))
        {
            Dispatch(new SetMisRadioGroupStateAction(state));
        }
    }

    private void HandlePropertyTextareaAction(
        IMisTextareaAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextareaInputAction inputAction && inputAction.IntentId == "prop-options")
        {
            Dispatch(new SetMisRadioGroupOptionsAction(ParseRadioGroupOptions(inputAction.Value ?? string.Empty)));
        }
    }

    private void HandleClearRadioGroupEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisRadioGroupEventsAction());
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }

    private static List<MisRadioOptionViewModel> ParseRadioGroupOptions(
        string rawText
    )
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [new MisRadioOptionViewModel("option-1", "Option 1")];
        }

        List<MisRadioOptionViewModel> options = [];
        string[] lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split('|', StringSplitOptions.TrimEntries);
            string value = parts.ElementAtOrDefault(0) ?? string.Empty;
            string label = parts.ElementAtOrDefault(1) ?? value;
            bool isDisabled = bool.TryParse(parts.ElementAtOrDefault(2), out bool parsedIsDisabled) && parsedIsDisabled;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            options.Add(new MisRadioOptionViewModel(value, string.IsNullOrWhiteSpace(label) ? value : label, isDisabled));
        }

        return options.Count == 0
            ? [new MisRadioOptionViewModel("option-1", "Option 1")]
            : options;
    }

    private static string FormatRadioGroupOptionsInput(
        IReadOnlyList<MisRadioOptionViewModel> options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
        {
            return string.Empty;
        }

        List<string> lines = [];
        foreach (MisRadioOptionViewModel option in options)
        {
            lines.Add($"{option.Value}|{option.Label}|{option.IsDisabled}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
