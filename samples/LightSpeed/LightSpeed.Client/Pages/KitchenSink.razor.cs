using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;
using LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisButton;
using Mississippi.Refraction.Components.Molecules.MisButton.Actions;
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

    private IReadOnlyList<string> TextInputEvents =>
        Select<MisTextInputKitchenSinkState, IReadOnlyList<string>>(MisTextInputKitchenSinkSelectors.GetEventLog);

    private MisTextInputViewModel TextInputModel =>
        Select<MisTextInputKitchenSinkState, MisTextInputViewModel>(MisTextInputKitchenSinkSelectors.GetViewModel);

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
}