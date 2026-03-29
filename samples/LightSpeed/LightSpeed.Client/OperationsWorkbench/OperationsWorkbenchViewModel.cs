using System.Collections.Generic;


namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Represents the complete state required to render the shared LightSpeed operations-workbench surface.
/// </summary>
public sealed record OperationsWorkbenchViewModel
{
    /// <summary>
    ///     Gets the number of actioned items in the complete queue.
    /// </summary>
    public required int ActionedItemCount { get; init; }

    /// <summary>
    ///     Gets the available brand options.
    /// </summary>
    public required IReadOnlyList<string> BrandOptions { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the selected work item can be actioned.
    /// </summary>
    public required bool CanActSelectedWorkItem { get; init; }

    /// <summary>
    ///     Gets the current brand identifier.
    /// </summary>
    public required string CurrentBrandId { get; init; }

    /// <summary>
    ///     Gets the current brand label.
    /// </summary>
    public required string CurrentBrandLabel { get; init; }

    /// <summary>
    ///     Gets the stage label for the currently selected work item.
    /// </summary>
    public required string CurrentSelectionStage { get; init; }

    /// <summary>
    ///     Gets the supported disposition options.
    /// </summary>
    public required IReadOnlyList<string> DispositionOptions { get; init; }

    /// <summary>
    ///     Gets the current draft analyst value.
    /// </summary>
    public required string DraftAssignedAnalyst { get; init; }

    /// <summary>
    ///     Gets the current draft analyst validation error.
    /// </summary>
    public string? DraftAssignedAnalystError { get; init; }

    /// <summary>
    ///     Gets the current draft disposition.
    /// </summary>
    public required string DraftDisposition { get; init; }

    /// <summary>
    ///     Gets the current draft response summary.
    /// </summary>
    public required string DraftResponseSummary { get; init; }

    /// <summary>
    ///     Gets the current draft response-summary validation error.
    /// </summary>
    public string? DraftResponseSummaryError { get; init; }

    /// <summary>
    ///     Gets the current draft review notes.
    /// </summary>
    public required string DraftReviewNotes { get; init; }

    /// <summary>
    ///     Gets the current draft review-notes validation error.
    /// </summary>
    public string? DraftReviewNotesError { get; init; }

    /// <summary>
    ///     Gets the banner feedback message.
    /// </summary>
    public string? FeedbackMessage { get; init; }

    /// <summary>
    ///     Gets the feedback tone used to style the banner.
    /// </summary>
    public required string FeedbackTone { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the review dialog is open.
    /// </summary>
    public required bool IsReviewDialogOpen { get; init; }

    /// <summary>
    ///     Gets the number of queue items in the full collection.
    /// </summary>
    public required int QueueCount { get; init; }

    /// <summary>
    ///     Gets the number of ready items in the full collection.
    /// </summary>
    public required int ReadyItemCount { get; init; }

    /// <summary>
    ///     Gets the eyebrow copy for the current route.
    /// </summary>
    public required string RouteEyebrow { get; init; }

    /// <summary>
    ///     Gets the subtitle copy for the current route.
    /// </summary>
    public required string RouteSubtitle { get; init; }

    /// <summary>
    ///     Gets the current search text.
    /// </summary>
    public required string SearchText { get; init; }

    /// <summary>
    ///     Gets the selected stage filter.
    /// </summary>
    public required string SelectedStageFilter { get; init; }

    /// <summary>
    ///     Gets the currently selected work item.
    /// </summary>
    public OperationsWorkbenchItem? SelectedWorkItem { get; init; }

    /// <summary>
    ///     Gets the supported stage filter options.
    /// </summary>
    public required IReadOnlyList<string> StageFilterOptions { get; init; }

    /// <summary>
    ///     Gets the validation-summary messages.
    /// </summary>
    public required IReadOnlyList<string> ValidationMessages { get; init; }

    /// <summary>
    ///     Gets the queue items visible after filtering.
    /// </summary>
    public required IReadOnlyList<OperationsWorkbenchItem> VisibleWorkItems { get; init; }
}