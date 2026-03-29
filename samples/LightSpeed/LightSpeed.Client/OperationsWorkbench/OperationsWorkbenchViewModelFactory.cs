using System;
using System.Collections.Generic;
using System.Linq;


namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Creates shared operations-workbench view models from underlying page or store state.
/// </summary>
internal static class OperationsWorkbenchViewModelFactory
{
    /// <summary>
    ///     Creates the complete view model for the shared operations-workbench surface.
    /// </summary>
    /// <param name="allWorkItems">The full work-item collection.</param>
    /// <param name="searchText">The active search text.</param>
    /// <param name="selectedStageFilter">The active stage filter.</param>
    /// <param name="selectedWorkItemId">The selected work-item identifier.</param>
    /// <param name="currentBrandId">The active brand identifier.</param>
    /// <param name="routeEyebrow">The route eyebrow copy.</param>
    /// <param name="routeSubtitle">The route subtitle copy.</param>
    /// <param name="isReviewDialogOpen">True when the review dialog is open.</param>
    /// <param name="draftAssignedAnalyst">The draft assigned-analyst value.</param>
    /// <param name="draftAssignedAnalystError">The draft assigned-analyst error.</param>
    /// <param name="draftDisposition">The draft disposition value.</param>
    /// <param name="draftResponseSummary">The draft response-summary value.</param>
    /// <param name="draftResponseSummaryError">The draft response-summary error.</param>
    /// <param name="draftReviewNotes">The draft review-notes value.</param>
    /// <param name="draftReviewNotesError">The draft review-notes error.</param>
    /// <param name="feedbackMessage">The feedback message rendered in the shared telemetry strip.</param>
    /// <param name="feedbackTone">The feedback tone rendered by the shared telemetry strip.</param>
    /// <param name="validationMessages">The validation-summary messages.</param>
    /// <returns>The view model rendered by the shared operations-workbench surface.</returns>
    public static OperationsWorkbenchViewModel Create(
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems,
        string searchText,
        string selectedStageFilter,
        string? selectedWorkItemId,
        string currentBrandId,
        string routeEyebrow,
        string routeSubtitle,
        bool isReviewDialogOpen,
        string draftAssignedAnalyst,
        string? draftAssignedAnalystError,
        string draftDisposition,
        string draftResponseSummary,
        string? draftResponseSummaryError,
        string draftReviewNotes,
        string? draftReviewNotesError,
        string? feedbackMessage,
        string feedbackTone,
        IReadOnlyList<string> validationMessages
    )
    {
        IReadOnlyList<OperationsWorkbenchItem> visibleWorkItems = OperationsWorkbenchScenario.GetFilteredWorkItems(
            allWorkItems,
            searchText,
            selectedStageFilter);
        OperationsWorkbenchItem? selectedWorkItem = OperationsWorkbenchScenario.GetSelectedWorkItem(
            allWorkItems,
            visibleWorkItems,
            selectedWorkItemId);
        return new()
        {
            ActionedItemCount = allWorkItems.Count(item => string.Equals(
                item.Stage,
                OperationsWorkbenchScenario.ActionedStage,
                StringComparison.Ordinal)),
            BrandOptions = OperationsWorkbenchScenario.BrandOptions,
            CanActSelectedWorkItem = OperationsWorkbenchScenario.CanActWorkItem(selectedWorkItem),
            CurrentBrandId = currentBrandId,
            CurrentBrandLabel = OperationsWorkbenchScenario.GetBrandLabel(currentBrandId),
            CurrentSelectionStage = selectedWorkItem?.Stage ?? "None",
            DraftAssignedAnalyst = draftAssignedAnalyst,
            DraftAssignedAnalystError = draftAssignedAnalystError,
            DraftDisposition = draftDisposition,
            DraftResponseSummary = draftResponseSummary,
            DraftResponseSummaryError = draftResponseSummaryError,
            DraftReviewNotes = draftReviewNotes,
            DraftReviewNotesError = draftReviewNotesError,
            DispositionOptions = OperationsWorkbenchScenario.DispositionOptions,
            FeedbackMessage = feedbackMessage,
            FeedbackTone = feedbackTone,
            IsReviewDialogOpen = isReviewDialogOpen,
            QueueCount = allWorkItems.Count,
            ReadyItemCount = allWorkItems.Count(item => string.Equals(
                item.Stage,
                OperationsWorkbenchScenario.ReadyStage,
                StringComparison.Ordinal)),
            RouteEyebrow = routeEyebrow,
            RouteSubtitle = routeSubtitle,
            SearchText = searchText,
            SelectedStageFilter = selectedStageFilter,
            SelectedWorkItem = selectedWorkItem,
            StageFilterOptions = OperationsWorkbenchScenario.StageFilterOptions,
            ValidationMessages = validationMessages,
            VisibleWorkItems = visibleWorkItems,
        };
    }
}