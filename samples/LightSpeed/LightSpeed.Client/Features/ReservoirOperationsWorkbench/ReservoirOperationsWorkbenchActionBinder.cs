using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Maps UI events to Reservoir actions for the LightSpeed operations-workbench parity route.
/// </summary>
internal static class ReservoirOperationsWorkbenchActionBinder
{
    /// <summary>
    ///     Creates an action that applies the selected ready work item.
    /// </summary>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateApplyActionRequestedAction() =>
        new ReservoirOperationsWorkbenchActions.ApplyActionRequested();

    /// <summary>
    ///     Creates an action that updates the draft assigned-analyst value.
    /// </summary>
    /// <param name="value">The updated draft value.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateDraftAssignedAnalystChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.DraftAssignedAnalystChanged(value);

    /// <summary>
    ///     Creates an action that updates the draft disposition value.
    /// </summary>
    /// <param name="value">The updated draft value.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateDraftDispositionChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.DraftDispositionChanged(value);

    /// <summary>
    ///     Creates an action that updates the draft response-summary value.
    /// </summary>
    /// <param name="value">The updated draft value.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateDraftResponseSummaryChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.DraftResponseSummaryChanged(value);

    /// <summary>
    ///     Creates an action that updates the draft review-notes value.
    /// </summary>
    /// <param name="value">The updated draft value.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateDraftReviewNotesChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.DraftReviewNotesChanged(value);

    /// <summary>
    ///     Creates an action that closes the review dialog.
    /// </summary>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateReviewCanceledAction() => new ReservoirOperationsWorkbenchActions.ReviewCanceled();

    /// <summary>
    ///     Creates an action that opens the review dialog.
    /// </summary>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateReviewOpenedAction() => new ReservoirOperationsWorkbenchActions.ReviewOpened();

    /// <summary>
    ///     Creates an action that saves the draft review.
    /// </summary>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateReviewSavedAction() => new ReservoirOperationsWorkbenchActions.ReviewSaved();

    /// <summary>
    ///     Creates an action that updates the search text.
    /// </summary>
    /// <param name="value">The updated search text.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateSearchChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.SearchChanged(value);

    /// <summary>
    ///     Creates an action that selects a work item.
    /// </summary>
    /// <param name="workItemId">The selected work-item identifier.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateSelectionChangedAction(
        string workItemId
    ) =>
        new ReservoirOperationsWorkbenchActions.SelectionChanged(workItemId);

    /// <summary>
    ///     Creates an action that updates the stage filter.
    /// </summary>
    /// <param name="value">The updated stage filter.</param>
    /// <returns>The action to dispatch.</returns>
    public static IAction CreateStageFilterChangedAction(
        string value
    ) =>
        new ReservoirOperationsWorkbenchActions.StageFilterChanged(value);
}