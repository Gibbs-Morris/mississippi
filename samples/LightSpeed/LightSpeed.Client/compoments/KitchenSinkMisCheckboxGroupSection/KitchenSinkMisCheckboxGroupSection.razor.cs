using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;
using LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;
using Mississippi.Refraction.Components.Molecules.MisSwitchActions;
using Mississippi.Refraction.Components.Molecules.MisTextareaActions;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisCheckboxGroup demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisCheckboxGroupSection : StoreComponent
{
    private const string EventsSectionKey = "misCheckboxGroup";

    private IReadOnlyList<string> CheckboxGroupEvents =>
        Select<MisCheckboxGroupKitchenSinkState, IReadOnlyList<string>>(MisCheckboxGroupKitchenSinkSelectors.GetEventLog);

    private MisCheckboxGroupViewModel CheckboxGroupModel =>
        Select<MisCheckboxGroupKitchenSinkState, MisCheckboxGroupViewModel>(MisCheckboxGroupKitchenSinkSelectors.GetViewModel);

    private bool IsEventsOpen =>
        Select<KitchenSinkSectionUiState, bool>(
            state => KitchenSinkSectionUiSelectors.IsEventsOpen(state, EventsSectionKey));

    private static string FormatAction(
        IMisCheckboxGroupAction action
    ) =>
        action switch
        {
            MisCheckboxGroupChangedAction changed =>
                $"intent={changed.IntentId}, value={changed.Value}, isChecked={changed.IsChecked}, values=[{string.Join(", ", changed.Values)}]",
            MisCheckboxGroupFocusedAction focused =>
                $"intent={focused.IntentId}",
            MisCheckboxGroupBlurredAction blurred =>
                $"intent={blurred.IntentId}",
            _ => action.ToString() ?? string.Empty,
        };

    private Task HandleMisCheckboxGroupActionAsync(
        IMisCheckboxGroupAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatch(new RecordMisCheckboxGroupEventAction(action.GetType().Name, FormatAction(action)));

        // Sync state updates
        switch (action)
        {
            case MisCheckboxGroupChangedAction changed:
                Dispatch(new SetMisCheckboxGroupValuesAction(changed.Values));
                break;
        }

        return Task.CompletedTask;
    }

    private void HandlePropertyTextInputAction(
        IMisTextInputAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextInputInputAction inputAction)
        {
            string? value = string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value.Trim();
            switch (inputAction.IntentId)
            {
                case "prop-arialabel":
                    Dispatch(new SetMisCheckboxGroupAriaLabelAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisCheckboxGroupCssClassAction(value));
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
                Dispatch(new SetMisCheckboxGroupDisabledAction(checkedValue));
                break;
            case "prop-required":
                Dispatch(new SetMisCheckboxGroupRequiredAction(checkedValue));
                break;
        }
    }

    private void HandlePropertyTextareaAction(
        IMisTextareaAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is MisTextareaInputAction inputAction && inputAction.IntentId == "prop-options")
        {
            Dispatch(new SetMisCheckboxGroupOptionsAction(ParseOptions(inputAction.Value ?? string.Empty)));
        }
    }

    private void HandleToggleEvents()
    {
        Dispatch(new ToggleKitchenSinkSectionEventsAction(EventsSectionKey));
    }

    private void HandleClearEvents()
    {
        Dispatch(new ClearMisCheckboxGroupEventsAction());
    }

    private static List<MisCheckboxOptionViewModel> ParseOptions(
        string rawText
    )
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [new MisCheckboxOptionViewModel("option-1", "Option 1")];
        }

        List<MisCheckboxOptionViewModel> options = [];
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

            options.Add(new MisCheckboxOptionViewModel(value, string.IsNullOrWhiteSpace(label) ? value : label, isDisabled));
        }

        return options.Count == 0
            ? [new MisCheckboxOptionViewModel("option-1", "Option 1")]
            : options;
    }

    private static string FormatOptionsInput(
        IReadOnlyList<MisCheckboxOptionViewModel> options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
        {
            return string.Empty;
        }

        List<string> lines = [];
        foreach (MisCheckboxOptionViewModel option in options)
        {
            lines.Add($"{option.Value}|{option.Label}|{option.IsDisabled}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
