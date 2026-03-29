using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Refraction.Abstractions.Theme;


namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Contains the deterministic seed data and pure transformations used by the shared operations-workbench surface.
/// </summary>
internal static class OperationsWorkbenchScenario
{
    /// <summary>
    ///     The stage label used after a response has been applied.
    /// </summary>
    internal const string ActionedStage = "Actioned";

    /// <summary>
    ///     The stage-filter option that keeps all items visible.
    /// </summary>
    internal const string AllStageFilter = "All";

    /// <summary>
    ///     The default Refraction brand identifier for LightSpeed.
    /// </summary>
    internal const string DefaultBrand = "horizon";

    /// <summary>
    ///     The disposition that marks the work item ready for action.
    /// </summary>
    internal const string DispatchDisposition = "Dispatch";

    /// <summary>
    ///     The disposition that pauses the work item for manual review.
    /// </summary>
    internal const string HoldDisposition = "Hold";

    /// <summary>
    ///     The disposition that escalates the work item for further investigation.
    /// </summary>
    internal const string InvestigateDisposition = "Investigate";

    /// <summary>
    ///     The stage label used while a review is still pending.
    /// </summary>
    internal const string PendingReviewStage = "Pending review";

    /// <summary>
    ///     The stage label used when a work item is ready to be actioned.
    /// </summary>
    internal const string ReadyStage = "Ready";

    /// <summary>
    ///     Gets the supported brand options.
    /// </summary>
    internal static IReadOnlyList<string> BrandOptions { get; } = [DefaultBrand, "signal"];

    /// <summary>
    ///     Gets the supported disposition options.
    /// </summary>
    internal static IReadOnlyList<string> DispositionOptions { get; } =
        [DispatchDisposition, InvestigateDisposition, HoldDisposition];

    /// <summary>
    ///     Gets the supported stage-filter options.
    /// </summary>
    internal static IReadOnlyList<string> StageFilterOptions { get; } =
        [AllStageFilter, PendingReviewStage, ReadyStage, ActionedStage];

    /// <summary>
    ///     Applies the final action transition to a ready work item.
    /// </summary>
    /// <param name="item">The selected work item.</param>
    /// <returns>The updated work item.</returns>
    internal static OperationsWorkbenchItem ApplyAction(
        OperationsWorkbenchItem item
    ) =>
        item with
        {
            Stage = ActionedStage,
            LastCheckpoint = "Response package released",
        };

    /// <summary>
    ///     Applies a saved review draft to a work item.
    /// </summary>
    /// <param name="item">The selected work item.</param>
    /// <param name="assignedAnalyst">The draft assigned analyst.</param>
    /// <param name="disposition">The draft disposition.</param>
    /// <param name="responseSummary">The draft response summary.</param>
    /// <param name="reviewNotes">The draft review notes.</param>
    /// <returns>The updated work item.</returns>
    internal static OperationsWorkbenchItem ApplyReview(
        OperationsWorkbenchItem item,
        string assignedAnalyst,
        string disposition,
        string responseSummary,
        string reviewNotes
    ) =>
        item with
        {
            AssignedAnalyst = assignedAnalyst.Trim(),
            Disposition = disposition,
            LastCheckpoint = GetCheckpointForDisposition(disposition),
            ResponseSummary = responseSummary.Trim(),
            ReviewNotes = reviewNotes.Trim(),
            Stage = GetStageForDisposition(disposition),
        };

    /// <summary>
    ///     Determines whether the selected work item can be actioned.
    /// </summary>
    /// <param name="item">The selected work item.</param>
    /// <returns><see langword="true" /> when the selected work item is ready.</returns>
    internal static bool CanActWorkItem(
        OperationsWorkbenchItem? item
    ) =>
        string.Equals(item?.Stage, ReadyStage, StringComparison.Ordinal);

    /// <summary>
    ///     Creates the deterministic seed data used by both LightSpeed routes.
    /// </summary>
    /// <returns>The seeded work-item collection.</returns>
    internal static IReadOnlyList<OperationsWorkbenchItem> CreateSeedData() =>
    [
        new()
        {
            Id = "OPS-1042",
            Customer = "Northwind Health",
            Queue = "Cold chain recovery",
            Stage = PendingReviewStage,
            AssignedAnalyst = "Maya Chen",
            Disposition = InvestigateDisposition,
            ResponseSummary = "Confirm the refrigeration transfer window before releasing the dispatch order.",
            ReviewNotes =
                "Regional desk requested one more compliance check before the replacement unit leaves the depot.",
            LastCheckpoint = "Awaiting review sign-off",
        },
        new()
        {
            Id = "OPS-1047",
            Customer = "Litware Devices",
            Queue = "Harbor customs release",
            Stage = ReadyStage,
            AssignedAnalyst = "Jonas Keane",
            Disposition = DispatchDisposition,
            ResponseSummary = "Release the customs hold and route the bonded carrier to dock seven.",
            ReviewNotes = "Carrier slot reserved, import paperwork validated, and warehouse crew notified.",
            LastCheckpoint = "Ready for dispatch",
        },
        new()
        {
            Id = "OPS-1051",
            Customer = "Contoso Retail",
            Queue = "Store replenishment surge",
            Stage = ActionedStage,
            AssignedAnalyst = "Priya Raman",
            Disposition = DispatchDisposition,
            ResponseSummary = "Prioritized the replenishment convoy to stabilize the flagship store backlog.",
            ReviewNotes = "Completed on the previous shift; monitoring for follow-up returns only.",
            LastCheckpoint = "Response package released",
        },
        new()
        {
            Id = "OPS-1056",
            Customer = "Fabrikam Energy",
            Queue = "Field maintenance reroute",
            Stage = PendingReviewStage,
            AssignedAnalyst = "Ava Mercer",
            Disposition = HoldDisposition,
            ResponseSummary = "Hold the reroute until the maintenance permit is attached to the work packet.",
            ReviewNotes =
                "Permit request open with the district office and crew briefing paused until confirmation arrives.",
            LastCheckpoint = "Permit evidence required",
        },
    ];

    /// <summary>
    ///     Creates the Refraction theme selection for the requested brand.
    /// </summary>
    /// <param name="brandId">The brand identifier.</param>
    /// <returns>The configured theme selection.</returns>
    internal static RefractionThemeSelection CreateThemeSelection(
        string brandId
    ) =>
        new()
        {
            BrandId = new RefractionBrandId(brandId),
            Contrast = RefractionContrastMode.Standard,
            Density = RefractionDensity.Comfortable,
            Motion = RefractionMotionMode.Standard,
        };

    /// <summary>
    ///     Ensures the selected work item remains visible after filtering changes.
    /// </summary>
    /// <param name="allWorkItems">The full work-item collection.</param>
    /// <param name="searchText">The active search text.</param>
    /// <param name="selectedStageFilter">The active stage filter.</param>
    /// <param name="selectedWorkItemId">The current selected work-item identifier.</param>
    /// <returns>The selected work-item identifier that should remain active.</returns>
    internal static string? EnsureSelectedWorkItemIsVisible(
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems,
        string searchText,
        string selectedStageFilter,
        string? selectedWorkItemId
    )
    {
        IReadOnlyList<OperationsWorkbenchItem> visibleItems = GetFilteredWorkItems(
            allWorkItems,
            searchText,
            selectedStageFilter);
        if (visibleItems.Count == 0)
        {
            return null;
        }

        return selectedWorkItemId is null ||
               visibleItems.All(item => !string.Equals(item.Id, selectedWorkItemId, StringComparison.Ordinal))
            ? visibleItems[0].Id
            : selectedWorkItemId;
    }

    /// <summary>
    ///     Gets the user-facing brand label for a brand identifier.
    /// </summary>
    /// <param name="brandOption">The brand identifier.</param>
    /// <returns>The user-facing brand label.</returns>
    internal static string GetBrandLabel(
        string brandOption
    ) =>
        brandOption switch
        {
            DefaultBrand => "Horizon",
            "signal" => "Signal",
            var _ => brandOption,
        };

    /// <summary>
    ///     Gets the checkpoint text associated with a disposition.
    /// </summary>
    /// <param name="disposition">The reviewed disposition.</param>
    /// <returns>The checkpoint message.</returns>
    internal static string GetCheckpointForDisposition(
        string disposition
    ) =>
        disposition switch
        {
            DispatchDisposition => "Plan validated and ready to act",
            InvestigateDisposition => "Escalated for further investigation",
            HoldDisposition => "Held for manual review",
            var _ => "Plan updated",
        };

    /// <summary>
    ///     Filters the full work-item collection using the current search and stage filter.
    /// </summary>
    /// <param name="allWorkItems">The full work-item collection.</param>
    /// <param name="searchText">The active search text.</param>
    /// <param name="selectedStageFilter">The active stage filter.</param>
    /// <returns>The filtered work-item collection.</returns>
    internal static IReadOnlyList<OperationsWorkbenchItem> GetFilteredWorkItems(
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems,
        string searchText,
        string selectedStageFilter
    ) =>
        allWorkItems.Where(item => MatchesSearch(item, searchText) && MatchesStageFilter(item, selectedStageFilter))
            .ToList();

    /// <summary>
    ///     Resolves the selected work item from the visible queue or full collection.
    /// </summary>
    /// <param name="allWorkItems">The full work-item collection.</param>
    /// <param name="visibleWorkItems">The filtered visible collection.</param>
    /// <param name="selectedWorkItemId">The selected work-item identifier.</param>
    /// <returns>The selected work item, when one is available.</returns>
    internal static OperationsWorkbenchItem? GetSelectedWorkItem(
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems,
        IReadOnlyList<OperationsWorkbenchItem> visibleWorkItems,
        string? selectedWorkItemId
    ) =>
        visibleWorkItems.FirstOrDefault(item => string.Equals(item.Id, selectedWorkItemId, StringComparison.Ordinal)) ??
        allWorkItems.FirstOrDefault(item => string.Equals(item.Id, selectedWorkItemId, StringComparison.Ordinal));

    /// <summary>
    ///     Gets the CSS suffix used for a rendered stage badge.
    /// </summary>
    /// <param name="stage">The displayed stage value.</param>
    /// <returns>The stage badge CSS suffix.</returns>
    internal static string GetStageCssSuffix(
        string stage
    ) =>
        stage switch
        {
            PendingReviewStage => "pending-review",
            ReadyStage => "ready",
            ActionedStage => "actioned",
            var _ => "unknown",
        };

    /// <summary>
    ///     Gets the next stage associated with a disposition.
    /// </summary>
    /// <param name="disposition">The reviewed disposition.</param>
    /// <returns>The stage value to render after save.</returns>
    internal static string GetStageForDisposition(
        string disposition
    ) =>
        disposition switch
        {
            DispatchDisposition => ReadyStage,
            HoldDisposition => PendingReviewStage,
            InvestigateDisposition => PendingReviewStage,
            var _ => PendingReviewStage,
        };

    /// <summary>
    ///     Updates the selected work item in the collection.
    /// </summary>
    /// <param name="allWorkItems">The full work-item collection.</param>
    /// <param name="selectedWorkItemId">The selected work-item identifier.</param>
    /// <param name="update">The update transformation.</param>
    /// <returns>The updated work-item collection.</returns>
    internal static IReadOnlyList<OperationsWorkbenchItem> UpdateItem(
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems,
        string selectedWorkItemId,
        Func<OperationsWorkbenchItem, OperationsWorkbenchItem> update
    ) =>
        allWorkItems.Select(item => string.Equals(item.Id, selectedWorkItemId, StringComparison.Ordinal)
                ? update(item)
                : item)
            .ToList();

    /// <summary>
    ///     Validates the current review draft.
    /// </summary>
    /// <param name="assignedAnalyst">The draft assigned analyst.</param>
    /// <param name="disposition">The draft disposition.</param>
    /// <param name="responseSummary">The draft response summary.</param>
    /// <param name="reviewNotes">The draft review notes.</param>
    /// <returns>The validation result.</returns>
    internal static OperationsWorkbenchValidationResult ValidateDraft(
        string assignedAnalyst,
        string disposition,
        string responseSummary,
        string reviewNotes
    )
    {
        List<string> validationMessages = [];
        string? assignedAnalystError = null;
        string? responseSummaryError = null;
        string? reviewNotesError = null;
        if (string.IsNullOrWhiteSpace(assignedAnalyst))
        {
            assignedAnalystError = "Assigned analyst is required.";
            validationMessages.Add(assignedAnalystError);
        }

        if (string.IsNullOrWhiteSpace(responseSummary) || (responseSummary.Trim().Length < 18))
        {
            responseSummaryError = "Response summary must contain at least 18 characters.";
            validationMessages.Add(responseSummaryError);
        }

        if (string.Equals(disposition, HoldDisposition, StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(reviewNotes))
        {
            reviewNotesError = "Review notes are required when the work item is held.";
            validationMessages.Add(reviewNotesError);
        }

        return new()
        {
            AssignedAnalystError = assignedAnalystError,
            Messages = validationMessages,
            ResponseSummaryError = responseSummaryError,
            ReviewNotesError = reviewNotesError,
        };
    }

    private static bool MatchesSearch(
        OperationsWorkbenchItem item,
        string searchText
    )
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        string trimmedSearchText = searchText.Trim();
        return item.Id.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase) ||
               item.Customer.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase) ||
               item.Queue.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase) ||
               item.AssignedAnalyst.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase) ||
               item.ResponseSummary.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesStageFilter(
        OperationsWorkbenchItem item,
        string selectedStageFilter
    ) =>
        string.Equals(selectedStageFilter, AllStageFilter, StringComparison.Ordinal) ||
        string.Equals(item.Stage, selectedStageFilter, StringComparison.Ordinal);
}