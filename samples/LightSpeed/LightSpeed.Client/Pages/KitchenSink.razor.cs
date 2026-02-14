using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisButton;
using Mississippi.Refraction.Components.Molecules.MisButton.Actions;
using Mississippi.Refraction.Components.Molecules.MisCheckboxActions;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Pages;

/// <summary>
///     Kitchen Sink page for interactive control diagnostics.
/// </summary>
public sealed partial class KitchenSink
{
    private IReadOnlyList<string> ButtonEvents =>
        Select<MisButtonKitchenSinkState, IReadOnlyList<string>>(MisButtonKitchenSinkSelectors.GetEventLog);

    private MisButtonViewModel ButtonModel =>
        Select<MisButtonKitchenSinkState, MisButtonViewModel>(MisButtonKitchenSinkSelectors.GetViewModel);

    private IReadOnlyList<string> CheckboxEvents =>
        Select<MisCheckboxKitchenSinkState, IReadOnlyList<string>>(MisCheckboxKitchenSinkSelectors.GetEventLog);

    private MisCheckboxViewModel CheckboxModel =>
        Select<MisCheckboxKitchenSinkState, MisCheckboxViewModel>(MisCheckboxKitchenSinkSelectors.GetViewModel);

    private IReadOnlyList<string> TextInputEvents =>
        Select<MisTextInputKitchenSinkState, IReadOnlyList<string>>(MisTextInputKitchenSinkSelectors.GetEventLog);

    private MisTextInputViewModel TextInputModel =>
        Select<MisTextInputKitchenSinkState, MisTextInputViewModel>(MisTextInputKitchenSinkSelectors.GetViewModel);

    private IReadOnlyList<string> SelectEvents =>
        Select<MisSelectKitchenSinkState, IReadOnlyList<string>>(MisSelectKitchenSinkSelectors.GetEventLog);

    private MisSelectViewModel SelectModel =>
        Select<MisSelectKitchenSinkState, MisSelectViewModel>(MisSelectKitchenSinkSelectors.GetViewModel);

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
            _ => $"intent={action.IntentId}",
        };

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

    private static string FormatAction(
        IMisCheckboxAction action
    ) =>
        action switch
        {
            MisCheckboxInputAction input =>
                $"intent={input.IntentId}, checked={input.IsChecked}",
            MisCheckboxChangedAction changed =>
                $"intent={changed.IntentId}, checked={changed.IsChecked}",
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
            _ => $"intent={action.IntentId}",
        };

    private static string FormatAction(
        IMisSelectAction action
    ) =>
        action switch
        {
            MisSelectInputAction input =>
                $"intent={input.IntentId}, value={input.Value}",
            MisSelectChangedAction changed =>
                $"intent={changed.IntentId}, value={changed.Value}",
            MisSelectKeyDownAction keyDown =>
                $"intent={keyDown.IntentId}, key={keyDown.Key}, code={keyDown.Code}, repeat={keyDown.Repeat}, ctrl={keyDown.CtrlKey}, shift={keyDown.ShiftKey}, alt={keyDown.AltKey}, meta={keyDown.MetaKey}",
            MisSelectKeyUpAction keyUp =>
                $"intent={keyUp.IntentId}, key={keyUp.Key}, code={keyUp.Code}, ctrl={keyUp.CtrlKey}, shift={keyUp.ShiftKey}, alt={keyUp.AltKey}, meta={keyUp.MetaKey}",
            MisSelectPointerDownAction pointerDown =>
                $"intent={pointerDown.IntentId}, button={pointerDown.Button}, ctrl={pointerDown.CtrlKey}, shift={pointerDown.ShiftKey}, alt={pointerDown.AltKey}, meta={pointerDown.MetaKey}",
            MisSelectPointerUpAction pointerUp =>
                $"intent={pointerUp.IntentId}, button={pointerUp.Button}, ctrl={pointerUp.CtrlKey}, shift={pointerUp.ShiftKey}, alt={pointerUp.AltKey}, meta={pointerUp.MetaKey}",
            MisSelectFocusedAction focused => $"intent={focused.IntentId}",
            MisSelectBlurredAction blurred => $"intent={blurred.IntentId}",
            _ => $"intent={action.IntentId}",
        };

    private void HandleAriaLabelChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonAriaLabelAction(GetOptionalValue(args)));

    private void HandleCssClassChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonCssClassAction(GetOptionalValue(args)));

    private void HandleIntentIdChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonIntentIdAction(GetRequiredValue(args, "kitchen-sink.mis-button")));

    private void HandleIsDisabledChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonDisabledAction(ToBool(args)));

    private Task HandleMisButtonActionAsync(
        IMisButtonAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatch(new RecordMisButtonEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

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

    private Task HandleMisSelectActionAsync(
        IMisSelectAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        switch (action)
        {
            case MisSelectInputAction inputAction:
                Dispatch(new SetMisSelectValueAction(inputAction.Value));
                break;
            case MisSelectChangedAction changedAction:
                Dispatch(new SetMisSelectValueAction(changedAction.Value));
                break;
        }

        Dispatch(new RecordMisSelectEventAction(action.GetType().Name, FormatAction(action)));
        return Task.CompletedTask;
    }

    private void HandleTextChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonTextAction(GetRequiredValue(args, "Button")));

    private void HandleTitleChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonTitleAction(GetOptionalValue(args)));

    private void HandleTypeChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisButtonTypeAction(ToButtonType(args)));

    private void HandleInputAriaLabelChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputAriaLabelAction(GetOptionalValue(args)));

    private void HandleInputAutoCompleteChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputAutoCompleteAction(GetOptionalValue(args)));

    private void HandleInputCssClassChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputCssClassAction(GetOptionalValue(args)));

    private void HandleInputIntentIdChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputIntentIdAction(GetRequiredValue(args, "kitchen-sink.mis-text-input")));

    private void HandleInputIsDisabledChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputDisabledAction(ToBool(args)));

    private void HandleInputIsReadOnlyChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputReadOnlyAction(ToBool(args)));

    private void HandleInputPlaceholderChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputPlaceholderAction(GetOptionalValue(args)));

    private void HandleInputTitleChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputTitleAction(GetOptionalValue(args)));

    private void HandleInputTypeChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputTypeAction(ToTextInputType(args)));

    private void HandleInputValueChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisTextInputValueAction(args.Value?.ToString() ?? string.Empty));

    private void HandleClearEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisButtonEventsAction());
    }

    private void HandleClearTextInputEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisTextInputEventsAction());
    }

    private void HandleCheckboxAriaLabelChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxAriaLabelAction(GetOptionalValue(args)));

    private void HandleCheckboxCssClassChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxCssClassAction(GetOptionalValue(args)));

    private void HandleCheckboxIntentIdChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxIntentIdAction(GetRequiredValue(args, "kitchen-sink.mis-checkbox")));

    private void HandleCheckboxIsCheckedChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxCheckedAction(ToBool(args)));

    private void HandleCheckboxIsDisabledChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxDisabledAction(ToBool(args)));

    private void HandleCheckboxIsRequiredChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxRequiredAction(ToBool(args)));

    private void HandleCheckboxStateChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxStateAction(ToCheckboxState(args)));

    private void HandleCheckboxTitleChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxTitleAction(GetOptionalValue(args)));

    private void HandleCheckboxValueChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisCheckboxValueAction(GetRequiredValue(args, "true")));

    private void HandleClearCheckboxEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisCheckboxEventsAction());
    }

    private void HandleSelectAriaLabelChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectAriaLabelAction(GetOptionalValue(args)));

    private void HandleSelectCssClassChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectCssClassAction(GetOptionalValue(args)));

    private void HandleSelectIntentIdChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectIntentIdAction(GetRequiredValue(args, "kitchen-sink.mis-select")));

    private void HandleSelectIsDisabledChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectDisabledAction(ToBool(args)));

    private void HandleSelectIsRequiredChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectRequiredAction(ToBool(args)));

    private void HandleSelectOptionsChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectOptionsAction(ParseSelectOptions(args.Value?.ToString() ?? string.Empty)));

    private void HandleSelectPlaceholderChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectPlaceholderAction(GetOptionalValue(args)));

    private void HandleSelectStateChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectStateAction(ToSelectState(args)));

    private void HandleSelectTitleChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectTitleAction(GetOptionalValue(args)));

    private void HandleSelectValueChanged(
        ChangeEventArgs args
    ) =>
        Dispatch(new SetMisSelectValueAction(args.Value?.ToString() ?? string.Empty));

    private void HandleClearSelectEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisSelectEventsAction());
    }

    private static string? GetOptionalValue(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        string? value = args.Value?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GetRequiredValue(
        ChangeEventArgs args,
        string fallback
    ) =>
        GetOptionalValue(args) ?? fallback;

    private static bool ToBool(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Value is bool value)
        {
            return value;
        }

        return bool.TryParse(args.Value?.ToString(), out bool parsedValue) && parsedValue;
    }

    private static MisButtonType ToButtonType(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        string? value = args.Value?.ToString();
        if (Enum.TryParse(value, true, out MisButtonType buttonType))
        {
            return buttonType;
        }

        return MisButtonType.Button;
    }

    private static MisTextInputType ToTextInputType(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        string? value = args.Value?.ToString();
        if (Enum.TryParse(value, true, out MisTextInputType inputType))
        {
            return inputType;
        }

        return MisTextInputType.Text;
    }

    private static MisCheckboxState ToCheckboxState(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        string? value = args.Value?.ToString();
        if (Enum.TryParse(value, true, out MisCheckboxState state))
        {
            return state;
        }

        return MisCheckboxState.Default;
    }

    private static MisSelectState ToSelectState(
        ChangeEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        string? value = args.Value?.ToString();
        if (Enum.TryParse(value, true, out MisSelectState state))
        {
            return state;
        }

        return MisSelectState.Default;
    }

    private static List<MisSelectOptionViewModel> ParseSelectOptions(
        string rawText
    )
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [new MisSelectOptionViewModel("option-1", "Option 1")];
        }

        List<MisSelectOptionViewModel> options = [];
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

            options.Add(new MisSelectOptionViewModel(value, string.IsNullOrWhiteSpace(label) ? value : label, isDisabled));
        }

        return options.Count == 0
            ? [new MisSelectOptionViewModel("option-1", "Option 1")]
            : options;
    }

    private static string FormatSelectOptionsInput(
        IReadOnlyList<MisSelectOptionViewModel> options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
        {
            return string.Empty;
        }

        List<string> lines = [];
        foreach (MisSelectOptionViewModel option in options)
        {
            lines.Add($"{option.Value}|{option.Label}|{option.IsDisabled}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}