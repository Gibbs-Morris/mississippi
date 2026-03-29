using System;
using System.Linq;

using MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;
using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Tests for <see cref="ReservoirOperationsWorkbenchReducers" />.
/// </summary>
public sealed class ReservoirOperationsWorkbenchReducersTests
{
    private const string ValidAssignedAnalyst = "Iris Valdez";

    private const string ValidResponseSummary =
        "Dispatch the compliance-approved replacement unit before the evening cut-off.";

    private const string ValidReviewNotes =
        "Compliance cleared and the field team has the release packet.";

    /// <summary>
    ///     ApplyAction returns the existing state when no work item is selected.
    /// </summary>
    [Fact]
    public void ApplyActionReturnsExistingStateWhenSelectionIsMissing()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            SearchText = "missing",
            SelectedWorkItemId = null,
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.ApplyAction(
            state,
            new ReservoirOperationsWorkbenchActions.ApplyActionRequested());

        // Assert
        Assert.Same(state, result);
    }

    /// <summary>
    ///     ApplyAction returns a warning when the selected work item is not ready.
    /// </summary>
    [Fact]
    public void ApplyActionReturnsWarningWhenSelectedWorkItemIsNotReady()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            SelectedWorkItemId = "OPS-1042",
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.ApplyAction(
            state,
            new ReservoirOperationsWorkbenchActions.ApplyActionRequested());
        OperationsWorkbenchItem selectedWorkItem = result.AllWorkItems.Single(item =>
            string.Equals(item.Id, "OPS-1042", StringComparison.Ordinal));

        // Assert
        Assert.Equal(OperationsWorkbenchScenario.PendingReviewStage, selectedWorkItem.Stage);
        Assert.Equal(
            "OPS-1042 must be marked Ready before the response can be applied.",
            result.FeedbackMessage);
        Assert.Equal("warning", result.FeedbackTone);
    }

    /// <summary>
    ///     OpenReview returns the existing state when no visible work item is selected.
    /// </summary>
    [Fact]
    public void OpenReviewReturnsExistingStateWhenSelectionIsMissing()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            SearchText = "missing",
            SelectedWorkItemId = null,
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.OpenReview(
            state,
            new ReservoirOperationsWorkbenchActions.ReviewOpened());

        // Assert
        Assert.Same(state, result);
    }

    /// <summary>
    ///     SaveReview keeps the dialog open and reports validation details when the draft is invalid.
    /// </summary>
    [Fact]
    public void SaveReviewReturnsValidationMessagesWhenDraftIsInvalid()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            DraftAssignedAnalyst = string.Empty,
            DraftDisposition = OperationsWorkbenchScenario.HoldDisposition,
            DraftResponseSummary = "Too short",
            DraftReviewNotes = string.Empty,
            IsReviewDialogOpen = true,
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.SaveReview(
            state,
            new ReservoirOperationsWorkbenchActions.ReviewSaved());

        // Assert
        Assert.True(result.IsReviewDialogOpen);
        Assert.Equal("Assigned analyst is required.", result.DraftAssignedAnalystError);
        Assert.Equal(
            "Response summary must contain at least 18 characters.",
            result.DraftResponseSummaryError);
        Assert.Equal(
            "Review notes are required when the work item is held.",
            result.DraftReviewNotesError);
        Assert.Equal(
            [
                "Assigned analyst is required.",
                "Response summary must contain at least 18 characters.",
                "Review notes are required when the work item is held.",
            ],
            result.ValidationMessages);
        Assert.Equal("Resolve the validation errors before saving the review.", result.FeedbackMessage);
        Assert.Equal("warning", result.FeedbackTone);
    }

    /// <summary>
    ///     SaveReview returns the existing state when no visible work item is selected.
    /// </summary>
    [Fact]
    public void SaveReviewReturnsExistingStateWhenSelectionIsMissing()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            DraftAssignedAnalyst = ValidAssignedAnalyst,
            DraftDisposition = OperationsWorkbenchScenario.DispatchDisposition,
            DraftResponseSummary = ValidResponseSummary,
            DraftReviewNotes = ValidReviewNotes,
            SearchText = "missing",
            SelectedWorkItemId = null,
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.SaveReview(
            state,
            new ReservoirOperationsWorkbenchActions.ReviewSaved());

        // Assert
        Assert.Same(state, result);
    }

    /// <summary>
    ///     SaveReview reselects the next visible work item when the saved item leaves the active stage filter.
    /// </summary>
    [Fact]
    public void SaveReviewReselectsVisibleWorkItemWhenSavedItemLeavesCurrentFilter()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            DraftAssignedAnalyst = ValidAssignedAnalyst,
            DraftDisposition = OperationsWorkbenchScenario.DispatchDisposition,
            DraftResponseSummary = ValidResponseSummary,
            DraftReviewNotes = ValidReviewNotes,
            IsReviewDialogOpen = true,
            SelectedStageFilter = OperationsWorkbenchScenario.PendingReviewStage,
            SelectedWorkItemId = "OPS-1042",
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.SaveReview(
            state,
            new ReservoirOperationsWorkbenchActions.ReviewSaved());
        OperationsWorkbenchItem updatedWorkItem = result.AllWorkItems.Single(item =>
            string.Equals(item.Id, "OPS-1042", StringComparison.Ordinal));

        // Assert
        Assert.False(result.IsReviewDialogOpen);
        Assert.Equal(OperationsWorkbenchScenario.ReadyStage, updatedWorkItem.Stage);
        Assert.Equal("OPS-1056", result.SelectedWorkItemId);
        Assert.Equal("success", result.FeedbackTone);
        Assert.Equal("OPS-1042 updated. The work item is now Ready.", result.FeedbackMessage);
        Assert.Empty(result.ValidationMessages);
    }

    /// <summary>
    ///     SetStageFilter clears the selection when the new filter removes every visible work item.
    /// </summary>
    [Fact]
    public void SetStageFilterClearsSelectionWhenNoVisibleWorkItemsRemain()
    {
        // Arrange
        ReservoirOperationsWorkbenchState state = new()
        {
            SearchText = "Northwind",
            SelectedWorkItemId = "OPS-1042",
        };

        // Act
        ReservoirOperationsWorkbenchState result = ReservoirOperationsWorkbenchReducers.SetStageFilter(
            state,
            new ReservoirOperationsWorkbenchActions.StageFilterChanged(OperationsWorkbenchScenario.ReadyStage));

        // Assert
        Assert.Equal(OperationsWorkbenchScenario.ReadyStage, result.SelectedStageFilter);
        Assert.Null(result.SelectedWorkItemId);
    }
}
