using System.Threading.Tasks;

using Mississippi.Refraction.Abstractions.Theme;

using MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;
using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace MississippiSamples.LightSpeed.Client.Pages;

/// <summary>
///     Hosts the LightSpeed Reservoir-explicit parity route using the shared operations-workbench surface.
/// </summary>
public sealed partial class ReservoirOperationsWorkbenchPage
{
    private const string ReservoirRouteEyebrow = "Refraction reboot · Reservoir-explicit parity proof";

    private const string ReservoirRouteSubtitle =
        "Search, filter, select, review, edit, validate, act, and switch brands live through Reservoir-backed composition.";

    private RefractionThemeSelection ThemeSelection { get; set; } =
        OperationsWorkbenchScenario.CreateThemeSelection(OperationsWorkbenchScenario.DefaultBrand);

    private OperationsWorkbenchViewModel ViewModel =>
        ReservoirOperationsWorkbenchProjector.Project(
            State,
            ThemeSelection.BrandId?.Value ?? OperationsWorkbenchScenario.DefaultBrand,
            ReservoirRouteEyebrow,
            ReservoirRouteSubtitle);

    private Task HandleActionRequestedAsync()
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateApplyActionRequestedAction());
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
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateDraftAssignedAnalystChangedAction(value));
        return Task.CompletedTask;
    }

    private Task HandleDraftDispositionChangedAsync(
        string value
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateDraftDispositionChangedAction(value));
        return Task.CompletedTask;
    }

    private Task HandleDraftResponseSummaryChangedAsync(
        string value
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateDraftResponseSummaryChangedAction(value));
        return Task.CompletedTask;
    }

    private Task HandleDraftReviewNotesChangedAsync(
        string value
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateDraftReviewNotesChangedAction(value));
        return Task.CompletedTask;
    }

    private Task HandleReviewCanceledAsync()
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateReviewCanceledAction());
        return Task.CompletedTask;
    }

    private Task HandleReviewRequestedAsync()
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateReviewOpenedAction());
        return Task.CompletedTask;
    }

    private Task HandleReviewSavedAsync()
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateReviewSavedAction());
        return Task.CompletedTask;
    }

    private Task HandleSearchChangedAsync(
        string value
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateSearchChangedAction(value));
        return Task.CompletedTask;
    }

    private Task HandleSelectionChangedAsync(
        string workItemId
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateSelectionChangedAction(workItemId));
        return Task.CompletedTask;
    }

    private Task HandleStageFilterChangedAsync(
        string value
    )
    {
        Store.Dispatch(ReservoirOperationsWorkbenchActionBinder.CreateStageFilterChangedAction(value));
        return Task.CompletedTask;
    }
}