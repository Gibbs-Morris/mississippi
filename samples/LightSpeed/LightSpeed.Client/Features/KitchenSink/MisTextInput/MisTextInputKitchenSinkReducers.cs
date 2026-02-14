using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Reducers for the MisTextInput Kitchen Sink feature state.
/// </summary>
internal static class MisTextInputKitchenSinkReducers
{
    /// <summary>
    ///     Sets the current text input value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetValue(
        MisTextInputKitchenSinkState state,
        SetMisTextInputValueAction action
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

    /// <summary>
    ///     Sets the intent identifier.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetIntentId(
        MisTextInputKitchenSinkState state,
        SetMisTextInputIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-text-input"),
            },
        };
    }

    /// <summary>
    ///     Sets the optional aria label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetAriaLabel(
        MisTextInputKitchenSinkState state,
        SetMisTextInputAriaLabelAction action
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
    ///     Sets the optional placeholder.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetPlaceholder(
        MisTextInputKitchenSinkState state,
        SetMisTextInputPlaceholderAction action
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
    ///     Sets the optional title.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetTitle(
        MisTextInputKitchenSinkState state,
        SetMisTextInputTitleAction action
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
    ///     Sets the optional CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetCssClass(
        MisTextInputKitchenSinkState state,
        SetMisTextInputCssClassAction action
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
    ///     Sets the optional autocomplete value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetAutoComplete(
        MisTextInputKitchenSinkState state,
        SetMisTextInputAutoCompleteAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                AutoComplete = NormalizeOptional(action.AutoComplete),
            },
        };
    }

    /// <summary>
    ///     Sets whether the input is disabled.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetDisabled(
        MisTextInputKitchenSinkState state,
        SetMisTextInputDisabledAction action
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
    ///     Sets whether the input is read-only.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetReadOnly(
        MisTextInputKitchenSinkState state,
        SetMisTextInputReadOnlyAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                IsReadOnly = action.IsReadOnly,
            },
        };
    }

    /// <summary>
    ///     Sets the text input type.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState SetType(
        MisTextInputKitchenSinkState state,
        SetMisTextInputTypeAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                Type = action.Type,
            },
        };
    }

    /// <summary>
    ///     Appends an interaction event entry to the event log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The event recording action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextInputKitchenSinkState RecordEvent(
        MisTextInputKitchenSinkState state,
        RecordMisTextInputEventAction action
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
    public static MisTextInputKitchenSinkState ClearEvents(
        MisTextInputKitchenSinkState state,
        ClearMisTextInputEventsAction action
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
