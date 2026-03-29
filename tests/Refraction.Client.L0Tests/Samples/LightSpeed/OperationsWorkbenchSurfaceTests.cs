using System;
using System.Collections.Generic;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed;

/// <summary>
///     Verifies the shared LightSpeed operations-workbench surface used by the increment-5 leaf-control replacement slice.
/// </summary>
public sealed class OperationsWorkbenchSurfaceTests : BunitContext
{
    private static OperationsWorkbenchViewModel CreateViewModel(
        bool isReviewDialogOpen = false
    )
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
            isReviewDialogOpen,
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
    ///     OperationsWorkbenchSurface renders the increment-5 command buttons and status badges through the shared workbench
    ///     surface.
    /// </summary>
    [Fact]
    public void OperationsWorkbenchSurfaceRendersReplacementLeafControls()
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
        Assert.Equal(2, cut.FindAll(".rf-command-button").Count);
        Assert.Equal(5, cut.FindAll(".rf-status-badge").Count);
        Assert.NotNull(cut.Find(".rf-command-button.ls-command-button--secondary[data-testid='review-open']"));
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='apply-action']"));
        Assert.NotNull(cut.Find(".rf-status-badge[data-testid='selected-stage']"));
        Assert.Equal("OPS-1042", cut.Find("[data-testid='selected-id']").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("[data-testid^='queue-select-']"));
        Assert.Empty(cut.FindAll(".rf-telemetry-strip"));
        Assert.Empty(cut.FindAll(".rf-pane"));
    }

    /// <summary>
    ///     OperationsWorkbenchSurface renders the increment-5 command buttons inside the shared review dialog actions.
    /// </summary>
    [Fact]
    public void OperationsWorkbenchSurfaceRendersReplacementLeafControlsInsideReviewDialog()
    {
        // Act
        using IRenderedComponent<OperationsWorkbenchSurface> cut = RenderWorkbenchSurface(
            CreateViewModel(isReviewDialogOpen: true));

        // Assert
        Assert.NotNull(cut.Find("[data-testid='review-dialog']"));
        Assert.Equal(4, cut.FindAll(".rf-command-button").Count);
        Assert.Equal(5, cut.FindAll(".rf-status-badge").Count);
        Assert.Collection(
            cut.FindAll(".ls-review-dialog__actions .rf-command-button"),
            button => Assert.Equal("Cancel", button.TextContent.Trim()),
            button => Assert.Equal("Save review", button.TextContent.Trim()));
        Assert.NotNull(cut.Find(".ls-review-dialog__actions .rf-command-button[data-testid='review-save']"));
    }
}