using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Reducers for the MisCheckbox Kitchen Sink feature state.
/// </summary>
internal static class MisCheckboxKitchenSinkReducers
{
    /// <summary>
    ///     Sets whether the checkbox is checked.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetChecked(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxCheckedAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                IsChecked = action.IsChecked,
            },
        };
    }

    /// <summary>
    ///     Sets the intent identifier.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetIntentId(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-checkbox"),
            },
        };
    }

    /// <summary>
    ///     Sets the optional aria label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetAriaLabel(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxAriaLabelAction action
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
    ///     Sets the optional title value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetTitle(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxTitleAction action
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
    ///     Sets the optional CSS class value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetCssClass(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxCssClassAction action
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
    ///     Sets whether the checkbox is disabled.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetDisabled(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxDisabledAction action
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
    ///     Sets whether the checkbox is required.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetRequired(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxRequiredAction action
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
    ///     Sets the submitted value attribute.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetValue(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxValueAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                Value = NormalizeRequired(action.Value, "true"),
            },
        };
    }

    /// <summary>
    ///     Sets the semantic visual state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState SetState(
        MisCheckboxKitchenSinkState state,
        SetMisCheckboxStateAction action
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
    ///     Appends an interaction event entry to the event log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The event recording action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState RecordEvent(
        MisCheckboxKitchenSinkState state,
        RecordMisCheckboxEventAction action
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
    ///     Clears all logged events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxKitchenSinkState ClearEvents(
        MisCheckboxKitchenSinkState state,
        ClearMisCheckboxEventsAction action
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

    private static string NormalizeRequired(
        string? value,
        string fallback
    ) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
