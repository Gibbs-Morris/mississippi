using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace MississippiSamples.LightSpeed.Client.OperationsWorkbench;

/// <summary>
///     Renders the shared operations-workbench surface used by both LightSpeed proof routes.
/// </summary>
public sealed partial class OperationsWorkbenchSurface
{
    /// <summary>
    ///     Gets or sets the view model rendered by the shared operations-workbench surface.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public OperationsWorkbenchViewModel Model { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the event callback invoked when the action button is pressed.
    /// </summary>
    [Parameter]
    public EventCallback OnActionRequested { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the active brand changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnBrandChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the draft assigned analyst changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDraftAssignedAnalystChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the draft disposition changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDraftDispositionChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the draft response summary changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDraftResponseSummaryChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the draft review notes change.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDraftReviewNotesChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the review dialog is cancelled.
    /// </summary>
    [Parameter]
    public EventCallback OnReviewCanceled { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the review dialog is opened.
    /// </summary>
    [Parameter]
    public EventCallback OnReviewRequested { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the review dialog is saved.
    /// </summary>
    [Parameter]
    public EventCallback OnReviewSaved { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the search text changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSearchChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when a queue row is selected.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSelectionChanged { get; set; }

    /// <summary>
    ///     Gets or sets the event callback invoked when the stage filter changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnStageFilterChanged { get; set; }

    private IReadOnlyDictionary<string, object> SummaryStripAttributes { get; } = new Dictionary<string, object>
    {
        ["class"] = "ls-workbench__summary",
        ["data-testid"] = "summary-strip",
    };

    private Task HandleDraftDispositionChangedAsync(
        ChangeEventArgs args
    ) =>
        OnDraftDispositionChanged.InvokeAsync(
            args.Value?.ToString() ?? OperationsWorkbenchScenario.DispatchDisposition);

    private Task HandleDraftReviewNotesChangedAsync(
        ChangeEventArgs args
    ) =>
        OnDraftReviewNotesChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleStageFilterChangedAsync(
        ChangeEventArgs args
    ) =>
        OnStageFilterChanged.InvokeAsync(args.Value?.ToString() ?? OperationsWorkbenchScenario.AllStageFilter);

    private bool IsActiveBrand(
        string brandOption
    ) =>
        string.Equals(Model.CurrentBrandId, brandOption, StringComparison.Ordinal);
}