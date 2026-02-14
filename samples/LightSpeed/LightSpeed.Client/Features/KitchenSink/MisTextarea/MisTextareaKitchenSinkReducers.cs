using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Reducers for the MisTextarea Kitchen Sink feature state.
/// </summary>
internal static class MisTextareaKitchenSinkReducers
{
    /// <summary>
    ///     Sets the textarea value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetValue(
        MisTextareaKitchenSinkState state,
        SetMisTextareaValueAction action
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
    public static MisTextareaKitchenSinkState SetIntentId(
        MisTextareaKitchenSinkState state,
        SetMisTextareaIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-textarea"),
            },
        };
    }

    /// <summary>
    ///     Sets the optional aria label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetAriaLabel(
        MisTextareaKitchenSinkState state,
        SetMisTextareaAriaLabelAction action
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
    public static MisTextareaKitchenSinkState SetTitle(
        MisTextareaKitchenSinkState state,
        SetMisTextareaTitleAction action
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
    public static MisTextareaKitchenSinkState SetCssClass(
        MisTextareaKitchenSinkState state,
        SetMisTextareaCssClassAction action
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
    ///     Sets the optional placeholder value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetPlaceholder(
        MisTextareaKitchenSinkState state,
        SetMisTextareaPlaceholderAction action
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
    ///     Sets the textarea row count.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetRows(
        MisTextareaKitchenSinkState state,
        SetMisTextareaRowsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        int normalizedRows = action.Rows <= 0 ? 1 : action.Rows;
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Rows = normalizedRows,
            },
        };
    }

    /// <summary>
    ///     Sets whether the textarea is disabled.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetDisabled(
        MisTextareaKitchenSinkState state,
        SetMisTextareaDisabledAction action
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
    ///     Sets whether the textarea is read-only.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetReadOnly(
        MisTextareaKitchenSinkState state,
        SetMisTextareaReadOnlyAction action
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
    ///     Sets whether the textarea is required.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState SetRequired(
        MisTextareaKitchenSinkState state,
        SetMisTextareaRequiredAction action
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
    public static MisTextareaKitchenSinkState SetState(
        MisTextareaKitchenSinkState state,
        SetMisTextareaStateAction action
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
    ///     Appends a new interaction event entry to the log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The event recording action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState RecordEvent(
        MisTextareaKitchenSinkState state,
        RecordMisTextareaEventAction action
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
    ///     Clears all recorded interaction events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisTextareaKitchenSinkState ClearEvents(
        MisTextareaKitchenSinkState state,
        ClearMisTextareaEventsAction action
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
