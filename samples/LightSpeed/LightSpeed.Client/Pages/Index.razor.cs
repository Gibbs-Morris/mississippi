using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Abstractions.Theme;


namespace MississippiSamples.LightSpeed.Client.Pages;

/// <summary>
///     Hosts the first truthful base-only Refraction workbench route for LightSpeed.
/// </summary>
public sealed partial class Index : ComponentBase
{
    private const string ActionedStage = "Actioned";

    private const string AllStageFilter = "All";

    private const string DefaultBrand = "horizon";

    private const string DispatchDisposition = "Dispatch";

    private const string HoldDisposition = "Hold";

    private const string InvestigateDisposition = "Investigate";

    private const string PendingReviewStage = "Pending review";

    private const string ReadyStage = "Ready";

    private static IReadOnlyList<string> BrandOptions { get; } = new[] { DefaultBrand, "signal" };

    private static IReadOnlyList<string> DispositionOptions { get; } =
        new[] { DispatchDisposition, InvestigateDisposition, HoldDisposition };

    private static IReadOnlyList<string> StageFilterOptions { get; } =
        new[] { AllStageFilter, PendingReviewStage, ReadyStage, ActionedStage };

    private int ActionedItemCount =>
        AllWorkItems.Count(item => string.Equals(item.Stage, ActionedStage, StringComparison.Ordinal));

    private IReadOnlyList<WorkbenchItem> AllWorkItems { get; set; } = CreateSeedData();

    private bool CanActSelectedWorkItem => string.Equals(SelectedWorkItem?.Stage, ReadyStage, StringComparison.Ordinal);

    private string CurrentBrandLabel => GetBrandLabel(ThemeSelection.BrandId?.Value ?? DefaultBrand);

    private string CurrentSelectionStage => SelectedWorkItem?.Stage ?? "None";

    private string DraftAssignedAnalyst { get; set; } = string.Empty;

    private string? DraftAssignedAnalystError { get; set; }

    private string DraftDisposition { get; set; } = DispatchDisposition;

    private string DraftResponseSummary { get; set; } = string.Empty;

    private string? DraftResponseSummaryError { get; set; }

    private string DraftReviewNotes { get; set; } = string.Empty;

    private string? DraftReviewNotesError { get; set; }

    private string? FeedbackMessage { get; set; }

    private string FeedbackTone { get; set; } = "neutral";

    private bool IsReviewDialogOpen { get; set; }

    private int ReadyItemCount =>
        AllWorkItems.Count(item => string.Equals(item.Stage, ReadyStage, StringComparison.Ordinal));

    private string SearchText { get; set; } = string.Empty;

    private string SelectedStageFilter { get; set; } = AllStageFilter;

    private WorkbenchItem? SelectedWorkItem =>
        GetFilteredWorkItems()
            .FirstOrDefault(item => string.Equals(item.Id, SelectedWorkItemId, StringComparison.Ordinal)) ??
        AllWorkItems.FirstOrDefault(item => string.Equals(item.Id, SelectedWorkItemId, StringComparison.Ordinal));

    private string? SelectedWorkItemId { get; set; }

    private IReadOnlyDictionary<string, object> SummaryStripAttributes { get; } = new Dictionary<string, object>
    {
        ["class"] = "ls-workbench__summary",
        ["data-testid"] = "summary-strip",
    };

    private RefractionThemeSelection ThemeSelection { get; set; } = CreateThemeSelection(DefaultBrand);

    private IReadOnlyList<string> ValidationMessages { get; set; } = Array.Empty<string>();

    private static List<WorkbenchItem> CreateSeedData() =>
        new()
        {
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
        };

    private static RefractionThemeSelection CreateThemeSelection(
        string brandId
    ) =>
        new()
        {
            BrandId = new RefractionBrandId(brandId),
            Contrast = RefractionContrastMode.Standard,
            Density = RefractionDensity.Comfortable,
            Motion = RefractionMotionMode.Standard,
        };

    private static string GetBrandLabel(
        string brandOption
    ) =>
        brandOption switch
        {
            DefaultBrand => "Horizon",
            "signal" => "Signal",
            var _ => brandOption,
        };

    private static string GetCheckpointForDisposition(
        string disposition
    ) =>
        disposition switch
        {
            DispatchDisposition => "Plan validated and ready to act",
            InvestigateDisposition => "Escalated for further investigation",
            HoldDisposition => "Held for manual review",
            var _ => "Plan updated",
        };

    private static string GetStageCssSuffix(
        string stage
    ) =>
        stage switch
        {
            PendingReviewStage => "pending-review",
            ReadyStage => "ready",
            ActionedStage => "actioned",
            var _ => "unknown",
        };

    private static string GetStageForDisposition(
        string disposition
    ) =>
        disposition switch
        {
            DispatchDisposition => ReadyStage,
            HoldDisposition => PendingReviewStage,
            InvestigateDisposition => PendingReviewStage,
            var _ => PendingReviewStage,
        };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        EnsureSelectedWorkItemIsVisible();
    }

    private void EnsureSelectedWorkItemIsVisible()
    {
        List<WorkbenchItem> visibleItems = GetFilteredWorkItems();
        if (visibleItems.Count == 0)
        {
            SelectedWorkItemId = null;
            return;
        }

        if (SelectedWorkItemId is null ||
            visibleItems.All(item => !string.Equals(item.Id, SelectedWorkItemId, StringComparison.Ordinal)))
        {
            SelectedWorkItemId = visibleItems[0].Id;
        }
    }

    private List<WorkbenchItem> GetFilteredWorkItems() =>
        AllWorkItems.Where(item => MatchesSearch(item) && MatchesStageFilter(item)).ToList();

    private Task HandleActionRequestedAsync()
    {
        if (SelectedWorkItem is null)
        {
            return Task.CompletedTask;
        }

        if (!CanActSelectedWorkItem)
        {
            FeedbackMessage = $"{SelectedWorkItem.Id} must be marked Ready before the response can be applied.";
            FeedbackTone = "warning";
            return Task.CompletedTask;
        }

        string workItemId = SelectedWorkItem.Id;
        AllWorkItems = AllWorkItems.Select(item => string.Equals(item.Id, workItemId, StringComparison.Ordinal)
                ? item with
                {
                    Stage = ActionedStage,
                    LastCheckpoint = "Response package released",
                }
                : item)
            .ToList();
        FeedbackMessage = $"{workItemId} action completed and the response package is now live.";
        FeedbackTone = "success";
        return Task.CompletedTask;
    }

    private Task HandleBrandChangedAsync(
        string brandOption
    )
    {
        ThemeSelection = CreateThemeSelection(brandOption);
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
        ChangeEventArgs args
    )
    {
        DraftDisposition = args.Value?.ToString() ?? DispatchDisposition;
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
        ChangeEventArgs args
    )
    {
        DraftReviewNotes = args.Value?.ToString() ?? string.Empty;
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

        ValidationResult validationResult = ValidateDraft();
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
        string nextStage = GetStageForDisposition(DraftDisposition);
        string nextCheckpoint = GetCheckpointForDisposition(DraftDisposition);
        AllWorkItems = AllWorkItems.Select(item => string.Equals(item.Id, workItemId, StringComparison.Ordinal)
                ? item with
                {
                    AssignedAnalyst = DraftAssignedAnalyst.Trim(),
                    Disposition = DraftDisposition,
                    ResponseSummary = DraftResponseSummary.Trim(),
                    ReviewNotes = DraftReviewNotes.Trim(),
                    Stage = nextStage,
                    LastCheckpoint = nextCheckpoint,
                }
                : item)
            .ToList();
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
        ChangeEventArgs args
    )
    {
        SelectedStageFilter = args.Value?.ToString() ?? AllStageFilter;
        EnsureSelectedWorkItemIsVisible();
        return Task.CompletedTask;
    }

    private bool IsActiveBrand(
        string brandOption
    ) =>
        string.Equals(ThemeSelection.BrandId?.Value, brandOption, StringComparison.Ordinal);

    private bool MatchesSearch(
        WorkbenchItem item
    )
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        string searchText = SearchText.Trim();
        return item.Id.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.Customer.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.Queue.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.AssignedAnalyst.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.ResponseSummary.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesStageFilter(
        WorkbenchItem item
    ) =>
        string.Equals(SelectedStageFilter, AllStageFilter, StringComparison.Ordinal) ||
        string.Equals(item.Stage, SelectedStageFilter, StringComparison.Ordinal);

    private void ResetValidationState()
    {
        DraftAssignedAnalystError = null;
        DraftResponseSummaryError = null;
        DraftReviewNotesError = null;
        ValidationMessages = Array.Empty<string>();
    }

    private ValidationResult ValidateDraft()
    {
        List<string> validationMessages = [];
        string? assignedAnalystError = null;
        string? responseSummaryError = null;
        string? reviewNotesError = null;
        if (string.IsNullOrWhiteSpace(DraftAssignedAnalyst))
        {
            assignedAnalystError = "Assigned analyst is required.";
            validationMessages.Add(assignedAnalystError);
        }

        if (string.IsNullOrWhiteSpace(DraftResponseSummary) || (DraftResponseSummary.Trim().Length < 18))
        {
            responseSummaryError = "Response summary must contain at least 18 characters.";
            validationMessages.Add(responseSummaryError);
        }

        if (string.Equals(DraftDisposition, HoldDisposition, StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(DraftReviewNotes))
        {
            reviewNotesError = "Review notes are required when the work item is held.";
            validationMessages.Add(reviewNotesError);
        }

        return new(assignedAnalystError, responseSummaryError, reviewNotesError, validationMessages);
    }

    private sealed record ValidationResult(
        string? AssignedAnalystError,
        string? ResponseSummaryError,
        string? ReviewNotesError,
        IReadOnlyList<string> Messages
    )
    {
        public bool HasErrors => Messages.Count > 0;
    }

    private sealed record WorkbenchItem
    {
        public required string AssignedAnalyst { get; init; }

        public required string Customer { get; init; }

        public required string Disposition { get; init; }

        public required string Id { get; init; }

        public required string LastCheckpoint { get; init; }

        public required string Queue { get; init; }

        public required string ResponseSummary { get; init; }

        public required string ReviewNotes { get; init; }

        public required string Stage { get; init; }
    }
}