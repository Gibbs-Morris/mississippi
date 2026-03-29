using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Projects Reservoir state into the shared operations-workbench view model.
/// </summary>
internal static class ReservoirOperationsWorkbenchProjector
{
    /// <summary>
    ///     Projects Reservoir state into the shared operations-workbench view model.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="currentBrandId">The active brand identifier.</param>
    /// <param name="routeEyebrow">The route eyebrow copy.</param>
    /// <param name="routeSubtitle">The route subtitle copy.</param>
    /// <returns>The projected view model.</returns>
    public static OperationsWorkbenchViewModel Project(
        ReservoirOperationsWorkbenchState state,
        string currentBrandId,
        string routeEyebrow,
        string routeSubtitle
    ) =>
        OperationsWorkbenchViewModelFactory.Create(
            state.AllWorkItems,
            state.SearchText,
            state.SelectedStageFilter,
            state.SelectedWorkItemId,
            currentBrandId,
            routeEyebrow,
            routeSubtitle,
            state.IsReviewDialogOpen,
            state.DraftAssignedAnalyst,
            state.DraftAssignedAnalystError,
            state.DraftDisposition,
            state.DraftResponseSummary,
            state.DraftResponseSummaryError,
            state.DraftReviewNotes,
            state.DraftReviewNotesError,
            state.FeedbackMessage,
            state.FeedbackTone,
            state.ValidationMessages);
}