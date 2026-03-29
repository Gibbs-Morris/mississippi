using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Abstractions.Theme;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace MississippiSamples.LightSpeed.Client.Pages;

/// <summary>
///     Hosts the first truthful base-only Refraction workbench route for LightSpeed.
/// </summary>
public sealed partial class Index : ComponentBase
{
    private const string BaseRouteEyebrow = "Refraction reboot · base-only proof";

    private const string BaseRouteSubtitle =
        "Search, filter, select, review, edit, validate, act, and switch brands live without store coupling.";

    private IReadOnlyList<OperationsWorkbenchItem> AllWorkItems { get; set; } =
        OperationsWorkbenchScenario.CreateSeedData();

    private string DraftAssignedAnalyst { get; set; } = string.Empty;

    private string? DraftAssignedAnalystError { get; set; }

    private string DraftDisposition { get; set; } = OperationsWorkbenchScenario.DispatchDisposition;

    private string DraftResponseSummary { get; set; } = string.Empty;

    private string? DraftResponseSummaryError { get; set; }

    private string DraftReviewNotes { get; set; } = string.Empty;

    private string? DraftReviewNotesError { get; set; }

    private string? FeedbackMessage { get; set; }

    private string FeedbackTone { get; set; } = "neutral";

    private bool IsReviewDialogOpen { get; set; }

    private string SearchText { get; set; } = string.Empty;

    private string SelectedStageFilter { get; set; } = OperationsWorkbenchScenario.AllStageFilter;

    private OperationsWorkbenchItem? SelectedWorkItem =>
        OperationsWorkbenchScenario.GetSelectedWorkItem(
            AllWorkItems,
            OperationsWorkbenchScenario.GetFilteredWorkItems(AllWorkItems, SearchText, SelectedStageFilter),
            SelectedWorkItemId);

    private string? SelectedWorkItemId { get; set; }

    private RefractionThemeSelection ThemeSelection { get; set; } =
        OperationsWorkbenchScenario.CreateThemeSelection(OperationsWorkbenchScenario.DefaultBrand);

    private IReadOnlyList<string> ValidationMessages { get; set; } = Array.Empty<string>();

    private OperationsWorkbenchViewModel ViewModel =>
        OperationsWorkbenchViewModelFactory.Create(
            AllWorkItems,
            SearchText,
            SelectedStageFilter,
            SelectedWorkItemId,
            ThemeSelection.BrandId?.Value ?? OperationsWorkbenchScenario.DefaultBrand,
            BaseRouteEyebrow,
            BaseRouteSubtitle,
            IsReviewDialogOpen,
            DraftAssignedAnalyst,
            DraftAssignedAnalystError,
            DraftDisposition,
            DraftResponseSummary,
            DraftResponseSummaryError,
            DraftReviewNotes,
            DraftReviewNotesError,
            FeedbackMessage,
            FeedbackTone,
            ValidationMessages);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        EnsureSelectedWorkItemIsVisible();
    }

    private void EnsureSelectedWorkItemIsVisible()
    {
        SelectedWorkItemId = OperationsWorkbenchScenario.EnsureSelectedWorkItemIsVisible(
            AllWorkItems,
            SearchText,
            SelectedStageFilter,
            SelectedWorkItemId);
    }

    private Task HandleActionRequestedAsync()
    {
        if (SelectedWorkItem is null)
        {
            return Task.CompletedTask;
        }

        if (!OperationsWorkbenchScenario.CanActWorkItem(SelectedWorkItem))
        {
            FeedbackMessage = $"{SelectedWorkItem.Id} must be marked Ready before the response can be applied.";
            FeedbackTone = "warning";
            return Task.CompletedTask;
        }

        string workItemId = SelectedWorkItem.Id;
        AllWorkItems = OperationsWorkbenchScenario.UpdateItem(
            AllWorkItems,
            workItemId,
            OperationsWorkbenchScenario.ApplyAction);
        FeedbackMessage = $"{workItemId} action completed and the response package is now live.";
        FeedbackTone = "success";
        return Task.CompletedTask;
    }

    private Task HandleBrandChangedAsync(
        string brandOption
    )
    {
        ThemeSelection = OperationsWorkbenchScenario.CreateThemeSelection(brandOption);
        return Task.CompletedTask;
    }

    private Task HandleDraftAssignedAnalystChangedAsync(
        string value
    )
    {
        DraftAssignedAnalyst = value;
        DraftAssignedAnalystError = null;
        return Task.CompletedTask;
    }

    private Task HandleDraftDispositionChangedAsync(
        string value
    )
    {
        DraftDisposition = value;
        DraftReviewNotesError = null;
        return Task.CompletedTask;
    }

    private Task HandleDraftResponseSummaryChangedAsync(
        string value
    )
    {
        DraftResponseSummary = value;
        DraftResponseSummaryError = null;
        return Task.CompletedTask;
    }

    private Task HandleDraftReviewNotesChangedAsync(
        string value
    )
    {
        DraftReviewNotes = value;
        DraftReviewNotesError = null;
        return Task.CompletedTask;
    }

    private Task HandleReviewCanceledAsync()
    {
        IsReviewDialogOpen = false;
        ResetValidationState();
        return Task.CompletedTask;
    }

    private Task HandleReviewRequestedAsync()
    {
        if (SelectedWorkItem is null)
        {
            return Task.CompletedTask;
        }

        DraftAssignedAnalyst = SelectedWorkItem.AssignedAnalyst;
        DraftDisposition = SelectedWorkItem.Disposition;
        DraftResponseSummary = SelectedWorkItem.ResponseSummary;
        DraftReviewNotes = SelectedWorkItem.ReviewNotes;
        IsReviewDialogOpen = true;
        ResetValidationState();
        return Task.CompletedTask;
    }

    private Task HandleReviewSavedAsync()
    {
        if (SelectedWorkItem is null)
        {
            return Task.CompletedTask;
        }

        OperationsWorkbenchValidationResult validationResult = OperationsWorkbenchScenario.ValidateDraft(
            DraftAssignedAnalyst,
            DraftDisposition,
            DraftResponseSummary,
            DraftReviewNotes);
        ValidationMessages = validationResult.Messages;
        DraftAssignedAnalystError = validationResult.AssignedAnalystError;
        DraftResponseSummaryError = validationResult.ResponseSummaryError;
        DraftReviewNotesError = validationResult.ReviewNotesError;
        if (validationResult.HasErrors)
        {
            FeedbackMessage = "Resolve the validation errors before saving the review.";
            FeedbackTone = "warning";
            return Task.CompletedTask;
        }

        string workItemId = SelectedWorkItem.Id;
        string nextStage = OperationsWorkbenchScenario.GetStageForDisposition(DraftDisposition);
        AllWorkItems = OperationsWorkbenchScenario.UpdateItem(
            AllWorkItems,
            workItemId,
            item => OperationsWorkbenchScenario.ApplyReview(
                item,
                DraftAssignedAnalyst,
                DraftDisposition,
                DraftResponseSummary,
                DraftReviewNotes));
        IsReviewDialogOpen = false;
        ResetValidationState();
        FeedbackMessage = $"{workItemId} updated. The work item is now {nextStage}.";
        FeedbackTone = "success";
        EnsureSelectedWorkItemIsVisible();
        return Task.CompletedTask;
    }

    private Task HandleSearchChangedAsync(
        string value
    )
    {
        SearchText = value;
        EnsureSelectedWorkItemIsVisible();
        return Task.CompletedTask;
    }

    private Task HandleSelectionChangedAsync(
        string workItemId
    )
    {
        SelectedWorkItemId = workItemId;
        return Task.CompletedTask;
    }

    private Task HandleStageFilterChangedAsync(
        string value
    )
    {
        SelectedStageFilter = value;
        EnsureSelectedWorkItemIsVisible();
        return Task.CompletedTask;
    }

    private void ResetValidationState()
    {
        DraftAssignedAnalystError = null;
        DraftResponseSummaryError = null;
        DraftReviewNotesError = null;
        ValidationMessages = Array.Empty<string>();
    }
}