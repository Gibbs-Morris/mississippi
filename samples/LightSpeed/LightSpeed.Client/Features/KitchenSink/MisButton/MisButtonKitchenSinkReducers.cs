using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Reducers for the MisButton Kitchen Sink feature state.
/// </summary>
internal static class MisButtonKitchenSinkReducers
{
    /// <summary>
    ///     Clears all recorded interaction events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisButtonKitchenSinkState ClearEvents(
        MisButtonKitchenSinkState state,
        ClearMisButtonEventsAction action
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
    public static MisButtonKitchenSinkState RecordEvent(
        MisButtonKitchenSinkState state,
        RecordMisButtonEventAction action
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
    public static MisButtonKitchenSinkState SetAriaLabel(
        MisButtonKitchenSinkState state,
        SetMisButtonAriaLabelAction action
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
    public static MisButtonKitchenSinkState SetCssClass(
        MisButtonKitchenSinkState state,
        SetMisButtonCssClassAction action
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
    ///     Sets the button intent identifier.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisButtonKitchenSinkState SetIntentId(
        MisButtonKitchenSinkState state,
        SetMisButtonIntentIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IntentId = NormalizeRequired(action.IntentId, "kitchen-sink.mis-button"),
            },
        };
    }

    /// <summary>
    ///     Sets the disabled state of the button.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisButtonKitchenSinkState SetIsDisabled(
        MisButtonKitchenSinkState state,
        SetMisButtonDisabledAction action
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
    ///     Sets the optional title value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisButtonKitchenSinkState SetTitle(
        MisButtonKitchenSinkState state,
        SetMisButtonTitleAction action
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
    ///     Sets the button type.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisButtonKitchenSinkState SetType(
        MisButtonKitchenSinkState state,
        SetMisButtonTypeAction action
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

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(
        string? value,
        string fallback
    ) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}