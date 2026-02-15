using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Reducers for the MisSelect Kitchen Sink feature state.
/// </summary>
internal static class MisSelectKitchenSinkReducers
{
    /// <summary>
    ///     Clears all recorded interaction events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState ClearEvents(
        MisSelectKitchenSinkState state,
        ClearMisSelectEventsAction action
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
    ///     Appends a new interaction event entry to the log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The event recording action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState RecordEvent(
        MisSelectKitchenSinkState state,
        RecordMisSelectEventAction action
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
    public static MisSelectKitchenSinkState SetAriaLabel(
        MisSelectKitchenSinkState state,
        SetMisSelectAriaLabelAction action
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
    public static MisSelectKitchenSinkState SetCssClass(
        MisSelectKitchenSinkState state,
        SetMisSelectCssClassAction action
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
    ///     Sets whether the select is disabled.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState SetDisabled(
        MisSelectKitchenSinkState state,
        SetMisSelectDisabledAction action
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
    public static MisSelectKitchenSinkState SetIntentId(
        MisSelectKitchenSinkState state,
        SetMisSelectIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-select"),
            },
        };
    }

    /// <summary>
    ///     Sets select options.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState SetOptions(
        MisSelectKitchenSinkState state,
        SetMisSelectOptionsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        IReadOnlyList<MisSelectOptionViewModel> normalizedOptions = NormalizeOptions(action.Options);
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
    ///     Sets the optional placeholder value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState SetPlaceholder(
        MisSelectKitchenSinkState state,
        SetMisSelectPlaceholderAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Placeholder = NormalizeOptional(action.Placeholder),
            },
        };
    }

    /// <summary>
    ///     Sets whether the select is required.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState SetRequired(
        MisSelectKitchenSinkState state,
        SetMisSelectRequiredAction action
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
    public static MisSelectKitchenSinkState SetState(
        MisSelectKitchenSinkState state,
        SetMisSelectStateAction action
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
    public static MisSelectKitchenSinkState SetTitle(
        MisSelectKitchenSinkState state,
        SetMisSelectTitleAction action
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
    ///     Sets the currently selected value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSelectKitchenSinkState SetValue(
        MisSelectKitchenSinkState state,
        SetMisSelectValueAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Value = action.Value ?? string.Empty,
            },
        };
    }

    private static bool ContainsValue(
        IReadOnlyList<MisSelectOptionViewModel> options,
        string value
    ) =>
        options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal));

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<MisSelectOptionViewModel> NormalizeOptions(
        IReadOnlyList<MisSelectOptionViewModel>? options
    )
    {
        if (options is null || (options.Count == 0))
        {
            return
            [
                new("option-1", "Option 1"),
            ];
        }

        List<MisSelectOptionViewModel> normalizedOptions = [];
        foreach (MisSelectOptionViewModel option in options)
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