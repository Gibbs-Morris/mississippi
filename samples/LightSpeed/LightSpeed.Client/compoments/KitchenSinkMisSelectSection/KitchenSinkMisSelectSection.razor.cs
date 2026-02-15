using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisSelectActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextareaActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisSelect demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisSelectSection : StoreComponent
{
    private const string EventsSectionKey = "misSelect";

    private static IReadOnlyList<MisSelectOptionViewModel> SelectStateOptions { get; } = Enum
        .GetValues<MisSelectState>()
        .Select(s => new MisSelectOptionViewModel(s.ToString(), s.ToString()))
        .ToList();

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(state =>
            KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private IReadOnlyList<string> SelectEvents =>
        Select<MisSelectKitchenSinkState, IReadOnlyList<string>>(MisSelectKitchenSinkSelectors.GetEventLog);

    private MisSelectViewModel SelectModel =>
        Select<MisSelectKitchenSinkState, MisSelectViewModel>(MisSelectKitchenSinkSelectors.GetViewModel);

    private IReadOnlyList<MisSelectOptionViewModel> SelectValueOptions => [.. SelectModel.Options];

    private static string FormatAction(
        IMisSelectAction action
    ) =>
        action switch
        {
            MisSelectInputAction input => $"intent={input.IntentId}, value={input.Value}",
            MisSelectChangedAction changed => $"intent={changed.IntentId}, value={changed.Value}",
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
            var _ => $"intent={action.IntentId}",
        };

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

    private static List<MisSelectOptionViewModel> ParseSelectOptions(
        string rawText
    )
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [new("option-1", "Option 1")];
        }

        List<MisSelectOptionViewModel> options = [];
        string[] lines = rawText.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

            options.Add(new(value, string.IsNullOrWhiteSpace(label) ? value : label, isDisabled));
        }

        return options.Count == 0 ? [new("option-1", "Option 1")] : options;
    }

    private void HandleClearSelectEvents(
        MouseEventArgs args
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        Dispatch(new ClearMisSelectEventsAction());
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

    private void HandlePropertySelectAction(
        IMisSelectAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is not MisSelectChangedAction changedAction)
        {
            return;
        }

        switch (changedAction.IntentId)
        {
            case "prop-value":
                Dispatch(new SetMisSelectValueAction(changedAction.Value));
                break;
            case "prop-state" when Enum.TryParse(changedAction.Value, true, out MisSelectState state):
                Dispatch(new SetMisSelectStateAction(state));
                break;
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
            var _ => null,
        };
        if (isChecked is not bool checkedValue)
        {
            return;
        }

        switch (action.IntentId)
        {
            case "prop-disabled":
                Dispatch(new SetMisSelectDisabledAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisSelectRequiredAction(checkedValue));
                break;
        }
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
                    Dispatch(
                        new SetMisSelectIntentIdAction(
                            !string.IsNullOrWhiteSpace(inputAction.Value)
                                ? inputAction.Value
                                : "kitchen-sink.mis-select"));
                    break;
                case "prop-arialabel":
                    Dispatch(
                        new SetMisSelectAriaLabelAction(
                            string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-placeholder":
                    Dispatch(
                        new SetMisSelectPlaceholderAction(
                            string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-title":
                    Dispatch(
                        new SetMisSelectTitleAction(
                            string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
                case "prop-cssclass":
                    Dispatch(
                        new SetMisSelectCssClassAction(
                            string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value));
                    break;
            }
        }
    }

    private void HandlePropertyTextareaAction(
        IMisTextareaAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextareaInputAction inputAction && (inputAction.IntentId == "prop-options"))
        {
            Dispatch(new SetMisSelectOptionsAction(ParseSelectOptions(inputAction.Value ?? string.Empty)));
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }
}