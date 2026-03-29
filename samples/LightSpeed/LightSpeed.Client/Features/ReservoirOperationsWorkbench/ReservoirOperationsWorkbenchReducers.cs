using System;
using System.Collections.Generic;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Contains the pure Reservoir reducers that back the LightSpeed operations-workbench parity route.
/// </summary>
internal static class ReservoirOperationsWorkbenchReducers
{
    /// <summary>
    ///     Applies the final action transition to the selected ready work item.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState ApplyAction(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.ApplyActionRequested action
    )
    {
        OperationsWorkbenchItem? selectedWorkItem = GetSelectedWorkItem(state);
        if (selectedWorkItem is null)
        {
            return state;
        }

        if (!OperationsWorkbenchScenario.CanActWorkItem(selectedWorkItem))
        {
            return state with
            {
                FeedbackMessage = $"{selectedWorkItem.Id} must be marked Ready before the response can be applied.",
                FeedbackTone = "warning",
            };
        }

        return state with
        {
            AllWorkItems = OperationsWorkbenchScenario.UpdateItem(
                state.AllWorkItems,
                selectedWorkItem.Id,
                OperationsWorkbenchScenario.ApplyAction),
            FeedbackMessage = $"{selectedWorkItem.Id} action completed and the response package is now live.",
            FeedbackTone = "success",
        };
    }

    /// <summary>
    ///     Cancels the review dialog and clears validation state.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState CancelReview(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.ReviewCanceled action
    ) =>
        ClearValidation(state) with
        {
            IsReviewDialogOpen = false,
        };

    /// <summary>
    ///     Opens the review dialog for the selected work item.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState OpenReview(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.ReviewOpened action
    )
    {
        OperationsWorkbenchItem? selectedWorkItem = GetSelectedWorkItem(state);
        if (selectedWorkItem is null)
        {
            return state;
        }

        return ClearValidation(state) with
        {
            DraftAssignedAnalyst = selectedWorkItem.AssignedAnalyst,
            DraftDisposition = selectedWorkItem.Disposition,
            DraftResponseSummary = selectedWorkItem.ResponseSummary,
            DraftReviewNotes = selectedWorkItem.ReviewNotes,
            IsReviewDialogOpen = true,
        };
    }

    /// <summary>
    ///     Saves the current draft review.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SaveReview(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.ReviewSaved action
    )
    {
        OperationsWorkbenchItem? selectedWorkItem = GetSelectedWorkItem(state);
        if (selectedWorkItem is null)
        {
            return state;
        }

        OperationsWorkbenchValidationResult validationResult = OperationsWorkbenchScenario.ValidateDraft(
            state.DraftAssignedAnalyst,
            state.DraftDisposition,
            state.DraftResponseSummary,
            state.DraftReviewNotes);
        if (validationResult.HasErrors)
        {
            return state with
            {
                DraftAssignedAnalystError = validationResult.AssignedAnalystError,
                DraftResponseSummaryError = validationResult.ResponseSummaryError,
                DraftReviewNotesError = validationResult.ReviewNotesError,
                FeedbackMessage = "Resolve the validation errors before saving the review.",
                FeedbackTone = "warning",
                ValidationMessages = validationResult.Messages,
            };
        }

        IReadOnlyList<OperationsWorkbenchItem> allWorkItems = OperationsWorkbenchScenario.UpdateItem(
            state.AllWorkItems,
            selectedWorkItem.Id,
            item => OperationsWorkbenchScenario.ApplyReview(
                item,
                state.DraftAssignedAnalyst,
                state.DraftDisposition,
                state.DraftResponseSummary,
                state.DraftReviewNotes));
        string nextStage = OperationsWorkbenchScenario.GetStageForDisposition(state.DraftDisposition);
        return ClearValidation(state) with
        {
            AllWorkItems = allWorkItems,
            FeedbackMessage = $"{selectedWorkItem.Id} updated. The work item is now {nextStage}.",
            FeedbackTone = "success",
            IsReviewDialogOpen = false,
            SelectedWorkItemId = OperationsWorkbenchScenario.EnsureSelectedWorkItemIsVisible(
                allWorkItems,
                state.SearchText,
                state.SelectedStageFilter,
                state.SelectedWorkItemId),
        };
    }

    /// <summary>
    ///     Updates the draft assigned-analyst value.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetDraftAssignedAnalyst(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.DraftAssignedAnalystChanged action
    ) =>
        state with
        {
            DraftAssignedAnalyst = action.Value,
            DraftAssignedAnalystError = null,
        };

    /// <summary>
    ///     Updates the draft disposition value.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetDraftDisposition(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.DraftDispositionChanged action
    ) =>
        state with
        {
            DraftDisposition = action.Value,
            DraftReviewNotesError = null,
        };

    /// <summary>
    ///     Updates the draft response-summary value.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetDraftResponseSummary(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.DraftResponseSummaryChanged action
    ) =>
        state with
        {
            DraftResponseSummary = action.Value,
            DraftResponseSummaryError = null,
        };

    /// <summary>
    ///     Updates the draft review-notes value.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetDraftReviewNotes(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.DraftReviewNotesChanged action
    ) =>
        state with
        {
            DraftReviewNotes = action.Value,
            DraftReviewNotesError = null,
        };

    /// <summary>
    ///     Updates the active search text.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetSearchText(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.SearchChanged action
    ) =>
        state with
        {
            SearchText = action.SearchText,
            SelectedWorkItemId = OperationsWorkbenchScenario.EnsureSelectedWorkItemIsVisible(
                state.AllWorkItems,
                action.SearchText,
                state.SelectedStageFilter,
                state.SelectedWorkItemId),
        };

    /// <summary>
    ///     Updates the selected work-item identifier.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetSelectedWorkItem(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.SelectionChanged action
    ) =>
        state with
        {
            SelectedWorkItemId = action.WorkItemId,
        };

    /// <summary>
    ///     Updates the active stage filter.
    /// </summary>
    /// <param name="state">The current Reservoir state.</param>
    /// <param name="action">The dispatched action.</param>
    /// <returns>The updated Reservoir state.</returns>
    public static ReservoirOperationsWorkbenchState SetStageFilter(
        ReservoirOperationsWorkbenchState state,
        ReservoirOperationsWorkbenchActions.StageFilterChanged action
    ) =>
        state with
        {
            SelectedStageFilter = action.StageFilter,
            SelectedWorkItemId = OperationsWorkbenchScenario.EnsureSelectedWorkItemIsVisible(
                state.AllWorkItems,
                state.SearchText,
                action.StageFilter,
                state.SelectedWorkItemId),
        };

    private static ReservoirOperationsWorkbenchState ClearValidation(
        ReservoirOperationsWorkbenchState state
    ) =>
        state with
        {
            DraftAssignedAnalystError = null,
            DraftResponseSummaryError = null,
            DraftReviewNotesError = null,
            ValidationMessages = Array.Empty<string>(),
        };

    private static OperationsWorkbenchItem? GetSelectedWorkItem(
        ReservoirOperationsWorkbenchState state
    ) =>
        OperationsWorkbenchScenario.GetSelectedWorkItem(
            state.AllWorkItems,
            OperationsWorkbenchScenario.GetFilteredWorkItems(
                state.AllWorkItems,
                state.SearchText,
                state.SelectedStageFilter),
            state.SelectedWorkItemId);
}