using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Reducers for the MisCheckboxGroup Kitchen Sink feature state.
/// </summary>
internal static class MisCheckboxGroupKitchenSinkReducers
{
    /// <summary>
    ///     Updates the selected checkbox values.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState SetValues(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupValuesAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { Values = new HashSet<string>(action.Values ?? []) } };
    }

    /// <summary>
    ///     Updates the disabled state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState SetDisabled(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupDisabledAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsDisabled = action.IsDisabled } };
    }

    /// <summary>
    ///     Updates the required state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState SetRequired(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupRequiredAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsRequired = action.IsRequired } };
    }

    /// <summary>
    ///     Updates the ARIA label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState SetAriaLabel(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupAriaLabelAction action
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
    public static MisCheckboxGroupKitchenSinkState SetCssClass(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupCssClassAction action
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
    ///     Updates the available checkbox options.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState SetOptions(
        MisCheckboxGroupKitchenSinkState state,
        SetMisCheckboxGroupOptionsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        IReadOnlyList<MisCheckboxOptionViewModel> options = action.Options.Count > 0
            ? action.Options
            : [new MisCheckboxOptionViewModel("option-1", "Option 1")];
        return state with { ViewModel = state.ViewModel with { Options = options } };
    }

    /// <summary>
    ///     Records a component event in the log.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The record event action.</param>
    /// <returns>The updated state.</returns>
    public static MisCheckboxGroupKitchenSinkState RecordEvent(
        MisCheckboxGroupKitchenSinkState state,
        RecordMisCheckboxGroupEventAction action
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
    public static MisCheckboxGroupKitchenSinkState ClearEvents(
        MisCheckboxGroupKitchenSinkState state,
        ClearMisCheckboxGroupEventsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { EventLog = [], EventCount = 0 };
    }
}
