using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

/// <summary>
///     Selectors for deriving values from <see cref="KitchenSinkSectionUiState" />.
/// </summary>
internal static class KitchenSinkSectionUiSelectors
{
    /// <summary>
    ///     Gets whether the events drawer is open for a section.
    /// </summary>
    /// <param name="state">The current section UI state.</param>
    /// <param name="sectionKey">The section key.</param>
    /// <returns><see langword="true" /> when open; otherwise <see langword="false" />.</returns>
    public static bool IsEventsOpen(
        KitchenSinkSectionUiState state,
        string sectionKey
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);
        return state.EventsPanelOpenStates.TryGetValue(sectionKey, out bool isOpen) && isOpen;
    }
}