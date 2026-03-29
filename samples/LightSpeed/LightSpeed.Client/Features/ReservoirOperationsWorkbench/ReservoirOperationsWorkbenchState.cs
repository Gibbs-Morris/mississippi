using System;
using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.State;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Represents the Reservoir-backed state for the LightSpeed operations-workbench parity route.
/// </summary>
public sealed record ReservoirOperationsWorkbenchState : IFeatureState
{
    /// <summary>
    ///     Gets the unique Reservoir feature key for the LightSpeed parity route.
    /// </summary>
    public static string FeatureKey => "lightSpeed.reservoirOperationsWorkbench";

    private static IReadOnlyList<OperationsWorkbenchItem> SeedWorkItems { get; } =
        OperationsWorkbenchScenario.CreateSeedData();

    /// <summary>
    ///     Gets the full work-item collection.
    /// </summary>
    public IReadOnlyList<OperationsWorkbenchItem> AllWorkItems { get; init; } = SeedWorkItems;

    /// <summary>
    ///     Gets the draft assigned-analyst value.
    /// </summary>
    public string DraftAssignedAnalyst { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the draft assigned-analyst validation error.
    /// </summary>
    public string? DraftAssignedAnalystError { get; init; }

    /// <summary>
    ///     Gets the draft disposition value.
    /// </summary>
    public string DraftDisposition { get; init; } = OperationsWorkbenchScenario.DispatchDisposition;

    /// <summary>
    ///     Gets the draft response-summary value.
    /// </summary>
    public string DraftResponseSummary { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the draft response-summary validation error.
    /// </summary>
    public string? DraftResponseSummaryError { get; init; }

    /// <summary>
    ///     Gets the draft review-notes value.
    /// </summary>
    public string DraftReviewNotes { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the draft review-notes validation error.
    /// </summary>
    public string? DraftReviewNotesError { get; init; }

    /// <summary>
    ///     Gets the feedback message rendered in the shared telemetry strip.
    /// </summary>
    public string? FeedbackMessage { get; init; }

    /// <summary>
    ///     Gets the feedback tone rendered by the shared telemetry strip.
    /// </summary>
    public string FeedbackTone { get; init; } = "neutral";

    /// <summary>
    ///     Gets a value indicating whether the review dialog is open.
    /// </summary>
    public bool IsReviewDialogOpen { get; init; }

    /// <summary>
    ///     Gets the active search text.
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the active stage filter.
    /// </summary>
    public string SelectedStageFilter { get; init; } = OperationsWorkbenchScenario.AllStageFilter;

    /// <summary>
    ///     Gets the selected work-item identifier.
    /// </summary>
    public string? SelectedWorkItemId { get; init; } = SeedWorkItems[0].Id;

    /// <summary>
    ///     Gets the validation-summary messages.
    /// </summary>
    public IReadOnlyList<string> ValidationMessages { get; init; } = Array.Empty<string>();
}