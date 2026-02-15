using System;
using System.Collections.Generic;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

/// <summary>
///     Reducers for the Kitchen Sink section UI feature state.
/// </summary>
internal static class KitchenSinkSectionUiReducers
{
    /// <summary>
    ///     Toggles the events drawer open state for a section.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The toggle action.</param>
    /// <returns>The updated state.</returns>
    public static KitchenSinkSectionUiState ToggleEventsPanel(
        KitchenSinkSectionUiState state,
        ToggleKitchenSinkSectionEventsAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.SectionKey);

        Dictionary<string, bool> nextStates = new(state.EventsPanelOpenStates, StringComparer.Ordinal);
        bool isOpen = KitchenSinkSectionUiSelectors.IsEventsOpen(state, action.SectionKey);
        nextStates[action.SectionKey] = !isOpen;

        return state with
        {
            EventsPanelOpenStates = nextStates,
        };
    }
}
