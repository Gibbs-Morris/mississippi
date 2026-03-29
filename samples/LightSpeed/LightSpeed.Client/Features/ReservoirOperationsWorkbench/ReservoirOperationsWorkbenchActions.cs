using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Contains the Reservoir actions used by the LightSpeed operations-workbench parity route.
/// </summary>
internal static class ReservoirOperationsWorkbenchActions
{
    /// <summary>
    ///     Requests applying the currently selected ready work item.
    /// </summary>
    internal sealed record ApplyActionRequested : IAction;

    /// <summary>
    ///     Updates the draft assigned-analyst value.
    /// </summary>
    /// <param name="Value">The updated draft value.</param>
    internal sealed record DraftAssignedAnalystChanged(string Value) : IAction;

    /// <summary>
    ///     Updates the draft disposition value.
    /// </summary>
    /// <param name="Value">The updated draft value.</param>
    internal sealed record DraftDispositionChanged(string Value) : IAction;

    /// <summary>
    ///     Updates the draft response-summary value.
    /// </summary>
    /// <param name="Value">The updated draft value.</param>
    internal sealed record DraftResponseSummaryChanged(string Value) : IAction;

    /// <summary>
    ///     Updates the draft review-notes value.
    /// </summary>
    /// <param name="Value">The updated draft value.</param>
    internal sealed record DraftReviewNotesChanged(string Value) : IAction;

    /// <summary>
    ///     Closes the review dialog without saving.
    /// </summary>
    internal sealed record ReviewCanceled : IAction;

    /// <summary>
    ///     Opens the review dialog for the selected work item.
    /// </summary>
    internal sealed record ReviewOpened : IAction;

    /// <summary>
    ///     Persists the current draft review.
    /// </summary>
    internal sealed record ReviewSaved : IAction;

    /// <summary>
    ///     Updates the search text.
    /// </summary>
    /// <param name="SearchText">The updated search text.</param>
    internal sealed record SearchChanged(string SearchText) : IAction;

    /// <summary>
    ///     Selects a work item by identifier.
    /// </summary>
    /// <param name="WorkItemId">The selected work-item identifier.</param>
    internal sealed record SelectionChanged(string WorkItemId) : IAction;

    /// <summary>
    ///     Updates the active stage filter.
    /// </summary>
    /// <param name="StageFilter">The selected stage filter.</param>
    internal sealed record StageFilterChanged(string StageFilter) : IAction;
}