using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Reducers for the MisPasswordInput Kitchen Sink feature state.
/// </summary>
internal static class MisPasswordInputKitchenSinkReducers
{
    /// <summary>
    ///     Updates the password input value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetValue(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputValueAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { Value = action.Value ?? string.Empty } };
    }

    /// <summary>
    ///     Updates the password visibility toggle.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetIsPasswordVisible(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputVisibilityAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsPasswordVisible = action.IsVisible } };
    }

    /// <summary>
    ///     Updates the disabled state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetDisabled(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputDisabledAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsDisabled = action.IsDisabled } };
    }

    /// <summary>
    ///     Updates the placeholder text.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetPlaceholder(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputPlaceholderAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Placeholder = string.IsNullOrWhiteSpace(action.Placeholder) ? null : action.Placeholder.Trim(),
            },
        };
    }

    /// <summary>
    ///     Records a component event in the log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The record event action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState RecordEvent(
        MisPasswordInputKitchenSinkState state,
        RecordMisPasswordInputEventAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        string[] newLog = [.. state.EventLog, $"{action.EventName}: {action.Details}"];
        return state with { EventLog = newLog, EventCount = newLog.Length };
    }

    /// <summary>
    ///     Clears all recorded events.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The clear action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState ClearEvents(
        MisPasswordInputKitchenSinkState state,
        ClearMisPasswordInputEventsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { EventLog = [], EventCount = 0 };
    }

    /// <summary>
    ///     Updates the ARIA label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetAriaLabel(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputAriaLabelAction action
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
    public static MisPasswordInputKitchenSinkState SetCssClass(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputCssClassAction action
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
    ///     Updates the read-only state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetReadOnly(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputReadOnlyAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsReadOnly = action.IsReadOnly } };
    }

    /// <summary>
    ///     Updates the validation state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisPasswordInputKitchenSinkState SetState(
        MisPasswordInputKitchenSinkState state,
        SetMisPasswordInputStateAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { State = action.State } };
    }
}
