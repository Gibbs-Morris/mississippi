using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Reducers for the MisSearchInput Kitchen Sink feature state.
/// </summary>
internal static class MisSearchInputKitchenSinkReducers
{
    /// <summary>
    ///     Clears all recorded events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState ClearEvents(
        MisSearchInputKitchenSinkState state,
        ClearMisSearchInputEventsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            EventLog = [],
            EventCount = 0,
        };
    }

    /// <summary>
    ///     Records a component event in the log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The record event action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState RecordEvent(
        MisSearchInputKitchenSinkState state,
        RecordMisSearchInputEventAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        string[] newLog = [.. state.EventLog, $"{action.EventName}: {action.Details}"];
        return state with
        {
            EventLog = newLog,
            EventCount = newLog.Length,
        };
    }

    /// <summary>
    ///     Updates the ARIA label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState SetAriaLabel(
        MisSearchInputKitchenSinkState state,
        SetMisSearchInputAriaLabelAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                AriaLabel = string.IsNullOrWhiteSpace(action.AriaLabel) ? null : action.AriaLabel.Trim(),
            },
        };
    }

    /// <summary>
    ///     Updates the CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState SetCssClass(
        MisSearchInputKitchenSinkState state,
        SetMisSearchInputCssClassAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                CssClass = string.IsNullOrWhiteSpace(action.CssClass) ? null : action.CssClass.Trim(),
            },
        };
    }

    /// <summary>
    ///     Updates the disabled state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState SetDisabled(
        MisSearchInputKitchenSinkState state,
        SetMisSearchInputDisabledAction action
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
    ///     Updates the placeholder text.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState SetPlaceholder(
        MisSearchInputKitchenSinkState state,
        SetMisSearchInputPlaceholderAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Placeholder = string.IsNullOrWhiteSpace(action.Placeholder) ? "Search..." : action.Placeholder.Trim(),
            },
        };
    }

    /// <summary>
    ///     Updates the search input value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisSearchInputKitchenSinkState SetValue(
        MisSearchInputKitchenSinkState state,
        SetMisSearchInputValueAction action
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
}