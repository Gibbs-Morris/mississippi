using System;
using System.Collections.Generic;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed;

/// <summary>
///     Verifies the shared LightSpeed operations-workbench surface used by the increment-4 replacement slice.
/// </summary>
public sealed class OperationsWorkbenchSurfaceTests : BunitContext
{
    private static OperationsWorkbenchViewModel CreateViewModel()
    {
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems = OperationsWorkbenchScenario.CreateSeedData();
        OperationsWorkbenchItem selectedWorkItem = allWorkItems[0];

        return OperationsWorkbenchViewModelFactory.Create(
            allWorkItems,
            string.Empty,
            OperationsWorkbenchScenario.AllStageFilter,
            selectedWorkItem.Id,
            OperationsWorkbenchScenario.DefaultBrand,
            "Operational readiness",
            "Review the active queue and respond with a stable action plan.",
            false,
            selectedWorkItem.AssignedAnalyst,
            null,
            selectedWorkItem.Disposition,
            selectedWorkItem.ResponseSummary,
            null,
            selectedWorkItem.ReviewNotes,
            null,
            null,
            "info",
            []);
    }

    private IRenderedComponent<OperationsWorkbenchSurface> RenderWorkbenchSurface(
        OperationsWorkbenchViewModel? model = null
    )
    {
        Services.AddLogging();
        Services.AddRefraction();
        JSInterop.Mode = JSRuntimeMode.Loose;

        return Render<OperationsWorkbenchSurface>(parameters => parameters.Add(
            component => component.Model,
            model ?? CreateViewModel()));
    }

    /// <summary>
    ///     OperationsWorkbenchSurface renders the increment-4 status-summary bar and surface panels instead of the retired HUD-era primitives.
    /// </summary>
    [Fact]
    public void OperationsWorkbenchSurfaceRendersReplacementWorkbenchShell()
    {
        // Act
        using IRenderedComponent<OperationsWorkbenchSurface> cut = RenderWorkbenchSurface();

        // Assert
        Assert.NotNull(cut.Find(".rf-status-summary-bar.ls-workbench__summary[data-testid='status-summary-bar']"));
        Assert.Equal("status", cut.Find(".rf-status-summary-bar").GetAttribute("role"));
        Assert.Contains("Queue 4", cut.Find(".rf-status-summary-bar").TextContent, StringComparison.Ordinal);
        Assert.Contains("Ready 1", cut.Find(".rf-status-summary-bar").TextContent, StringComparison.Ordinal);
        Assert.Contains("Actioned 1", cut.Find(".rf-status-summary-bar").TextContent, StringComparison.Ordinal);
        Assert.Contains(
            "Selected Pending review",
            cut.Find(".rf-status-summary-bar").TextContent,
            StringComparison.Ordinal);
        Assert.Contains("Theme Horizon", cut.Find(".rf-status-summary-bar").TextContent, StringComparison.Ordinal);
        Assert.Equal(2, cut.FindAll(".rf-surface-panel").Count);
        Assert.Collection(
            cut.FindAll(".rf-surface-panel__title"),
            title => Assert.Equal("Operations queue", title.TextContent.Trim()),
            title => Assert.Equal("Selected review", title.TextContent.Trim()));
        Assert.Single(cut.FindAll(".rf-surface-panel__footer"));
        Assert.NotNull(cut.Find("[data-testid='review-open']"));
        Assert.NotNull(cut.Find("[data-testid='apply-action']"));
        Assert.Equal("OPS-1042", cut.Find("[data-testid='selected-id']").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("[data-testid^='queue-select-']"));
        Assert.Empty(cut.FindAll(".rf-telemetry-strip"));
        Assert.Empty(cut.FindAll(".rf-pane"));
    }
}