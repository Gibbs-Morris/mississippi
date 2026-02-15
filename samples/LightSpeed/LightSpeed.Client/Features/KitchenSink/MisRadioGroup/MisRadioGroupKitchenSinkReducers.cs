using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Reducers for the MisRadioGroup Kitchen Sink feature state.
/// </summary>
internal static class MisRadioGroupKitchenSinkReducers
{
    /// <summary>
    ///     Clears all logged events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState ClearEvents(
        MisRadioGroupKitchenSinkState state,
        ClearMisRadioGroupEventsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            EventCount = 0,
            EventLog = [],
        };
    }

    /// <summary>
    ///     Appends an interaction event entry to the event log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The event recording action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState RecordEvent(
        MisRadioGroupKitchenSinkState state,
        RecordMisRadioGroupEventAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        int nextEventNumber = state.EventCount + 1;
        string entry = $"{nextEventNumber:000}: {action.EventName} - {action.EventDetails}";
        return state with
        {
            EventCount = nextEventNumber,
            EventLog = [.. state.EventLog, entry],
        };
    }

    /// <summary>
    ///     Sets the optional aria label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetAriaLabel(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupAriaLabelAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                AriaLabel = NormalizeOptional(action.AriaLabel),
            },
        };
    }

    /// <summary>
    ///     Sets the optional CSS class value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetCssClass(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupCssClassAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                CssClass = NormalizeOptional(action.CssClass),
            },
        };
    }

    /// <summary>
    ///     Sets whether the radio group is disabled.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetDisabled(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupDisabledAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IsDisabled = action.IsDisabled,
            },
        };
    }

    /// <summary>
    ///     Sets the intent identifier.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetIntentId(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-radio-group"),
            },
        };
    }

    /// <summary>
    ///     Sets radio options.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetOptions(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupOptionsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        List<MisRadioOptionViewModel> normalizedOptions = NormalizeOptions(action.Options);
        string normalizedValue = state.ViewModel.Value;
        if (!string.IsNullOrEmpty(normalizedValue) && !ContainsValue(normalizedOptions, normalizedValue))
        {
            normalizedValue = string.Empty;
        }

        return state with
        {
            ViewModel = state.ViewModel with
            {
                Options = normalizedOptions,
                Value = normalizedValue,
            },
        };
    }

    /// <summary>
    ///     Sets whether the radio group is required.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetRequired(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupRequiredAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IsRequired = action.IsRequired,
            },
        };
    }

    /// <summary>
    ///     Sets the semantic visual state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetState(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupStateAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                State = action.State,
            },
        };
    }

    /// <summary>
    ///     Sets the optional title value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetTitle(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupTitleAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Title = NormalizeOptional(action.Title),
            },
        };
    }

    /// <summary>
    ///     Sets the selected value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisRadioGroupKitchenSinkState SetValue(
        MisRadioGroupKitchenSinkState state,
        SetMisRadioGroupValueAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        string normalizedValue = action.Value ?? string.Empty;
        if (!string.IsNullOrEmpty(normalizedValue) && !ContainsValue(state.ViewModel.Options, normalizedValue))
        {
            normalizedValue = string.Empty;
        }

        return state with
        {
            ViewModel = state.ViewModel with
            {
                Value = normalizedValue,
            },
        };
    }

    private static bool ContainsValue(
        IReadOnlyList<MisRadioOptionViewModel> options,
        string value
    ) =>
        options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal));

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<MisRadioOptionViewModel> NormalizeOptions(
        IReadOnlyList<MisRadioOptionViewModel>? options
    )
    {
        if (options is null || (options.Count == 0))
        {
            return
            [
                new("option-1", "Option 1"),
            ];
        }

        List<MisRadioOptionViewModel> normalizedOptions = [];
        foreach (MisRadioOptionViewModel option in options)
        {
            string normalizedValue = NormalizeRequired(option.Value, string.Empty);
            string normalizedLabel = NormalizeRequired(option.Label, normalizedValue);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                continue;
            }

            normalizedOptions.Add(new(normalizedValue, normalizedLabel, option.IsDisabled));
        }

        return normalizedOptions.Count == 0 ? [new("option-1", "Option 1")] : normalizedOptions;
    }

    private static string NormalizeRequired(
        string? value,
        string fallback
    ) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}