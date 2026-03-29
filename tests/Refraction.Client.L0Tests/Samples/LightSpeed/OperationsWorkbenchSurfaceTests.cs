using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;

using MississippiSamples.LightSpeed.Client.OperationsWorkbench;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed;

/// <summary>
///     Verifies the shared LightSpeed operations-workbench surface used by the bounded workbench replacement slices.
/// </summary>
public sealed class OperationsWorkbenchSurfaceTests : BunitContext
{
    private static OperationsWorkbenchViewModel CreateEmptyStateViewModel()
    {
        IReadOnlyList<OperationsWorkbenchItem> allWorkItems = OperationsWorkbenchScenario.CreateSeedData();
        OperationsWorkbenchItem draftWorkItem = allWorkItems[0];
        return OperationsWorkbenchViewModelFactory.Create(
            allWorkItems,
            "Northwind",
            OperationsWorkbenchScenario.ReadyStage,
            null,
            OperationsWorkbenchScenario.DefaultBrand,
            "Operational readiness",
            "Review the active queue and respond with a stable action plan.",
            false,
            draftWorkItem.AssignedAnalyst,
            null,
            draftWorkItem.Disposition,
            draftWorkItem.ResponseSummary,
            null,
            draftWorkItem.ReviewNotes,
            null,
            null,
            "info",
            []);
    }

    private static OperationsWorkbenchViewModel CreateViewModel(
        bool isReviewDialogOpen = false,
        string? feedbackMessage = null,
        string feedbackTone = "info"
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
            feedbackMessage,
            feedbackTone,
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
    ///     OperationsWorkbenchSurface renders the increment-6 replacement empty states through the shared workbench surface.
    /// </summary>
    [Fact]
    public void OperationsWorkbenchSurfaceRendersReplacementEmptyStates()
    {
        // Act
        using IRenderedComponent<OperationsWorkbenchSurface> cut = RenderWorkbenchSurface(CreateEmptyStateViewModel());

        // Assert
        Assert.Equal(2, cut.FindAll(".rf-empty-state").Count);
        Assert.NotNull(cut.Find(".rf-empty-state.ls-workbench__empty-state[data-testid='queue-empty-state']"));
        Assert.Contains(
            "No work items match the current search and stage filter.",
            cut.Find("[data-testid='queue-empty-state']").TextContent,
            StringComparison.Ordinal);
        Assert.NotNull(cut.Find(".rf-empty-state.ls-workbench__empty-state[data-testid='detail-empty-state']"));
        Assert.Contains(
            "Select a queue item to inspect the current response plan.",
            cut.Find("[data-testid='detail-empty-state']").TextContent,
            StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid^='queue-select-']"));
        Assert.True(cut.Find("[data-testid='review-open']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='apply-action']").HasAttribute("disabled"));
    }

    /// <summary>
    ///     OperationsWorkbenchSurface renders the increment-8 action bar with the replacement command buttons and status
    ///     badges
    ///     through the shared workbench surface.
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
        Assert.Single(cut.FindAll(".rf-action-bar.ls-workbench__detail-actions"));
        Assert.NotNull(cut.Find(".rf-action-bar.ls-workbench__detail-actions"));
        Assert.NotNull(cut.Find(".rf-command-button.ls-command-button--secondary[data-testid='review-open']"));
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='apply-action']"));
        Assert.False(cut.Find("[data-testid='review-open']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='apply-action']").HasAttribute("disabled"));
        Assert.NotNull(cut.Find(".rf-status-badge[data-testid='selected-stage']"));
        Assert.Equal("OPS-1042", cut.Find("[data-testid='selected-id']").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("[data-testid^='queue-select-']"));
        Assert.Empty(cut.FindAll(".rf-telemetry-strip"));
        Assert.Empty(cut.FindAll(".rf-pane"));
    }

    /// <summary>
    ///     OperationsWorkbenchSurface renders the increment-8 action bars with the replacement command buttons inside the
    ///     shared review dialog actions.
    /// </summary>
    [Fact]
    public void OperationsWorkbenchSurfaceRendersReplacementLeafControlsInsideReviewDialog()
    {
        // Act
        using IRenderedComponent<OperationsWorkbenchSurface> cut = RenderWorkbenchSurface(CreateViewModel(true));

        // Assert
        Assert.NotNull(cut.Find("[data-testid='review-dialog']"));
        Assert.Equal(4, cut.FindAll(".rf-command-button").Count);
        Assert.Equal(5, cut.FindAll(".rf-status-badge").Count);
        Assert.Single(cut.FindAll(".rf-action-bar.ls-workbench__detail-actions"));
        Assert.Single(cut.FindAll(".rf-action-bar.ls-review-dialog__actions"));
        Assert.NotNull(cut.Find(".rf-action-bar.ls-workbench__detail-actions"));
        Assert.NotNull(cut.Find(".rf-action-bar.ls-review-dialog__actions"));
        Assert.Collection(
            cut.FindAll(".rf-action-bar.ls-review-dialog__actions .rf-command-button"),
            button => Assert.Equal("Cancel", button.TextContent.Trim()),
            button => Assert.Equal("Save review", button.TextContent.Trim()));
        Assert.NotNull(
            cut.Find(".rf-action-bar.ls-review-dialog__actions .rf-command-button[data-testid='review-save']"));
    }

    /// <summary>
    ///     OperationsWorkbenchSurface renders the increment-7 replacement telemetry strip when feedback is present.
    /// </summary>
    /// <param name="feedbackTone">The feedback tone projected into the replacement telemetry strip.</param>
    /// <param name="feedbackMessage">The feedback message rendered by the replacement telemetry strip.</param>
    [Theory]
    [InlineData("info", "Operational readiness remains stable across the active queue.")]
    [InlineData("warning", "Resolve the validation errors before saving the review.")]
    [InlineData("success", "OPS-1042 updated. The work item is now Ready.")]
    public void OperationsWorkbenchSurfaceRendersReplacementTelemetryStrip(
        string feedbackTone,
        string feedbackMessage
    )
    {
        // Act
        using IRenderedComponent<OperationsWorkbenchSurface> cut = RenderWorkbenchSurface(
            CreateViewModel(feedbackMessage: feedbackMessage, feedbackTone: feedbackTone));
        IElement feedbackStrip = cut.Find("[data-testid='feedback-banner']");

        // Assert
        Assert.Contains("rf-telemetry-strip", feedbackStrip.ClassList);
        Assert.Contains("ls-workbench__feedback", feedbackStrip.ClassList);
        Assert.Contains($"ls-workbench__feedback--{feedbackTone}", feedbackStrip.ClassList);
        Assert.Equal("polite", feedbackStrip.GetAttribute("aria-live"));
        Assert.Equal("status", feedbackStrip.GetAttribute("role"));
        Assert.Equal(feedbackTone, feedbackStrip.GetAttribute("data-state"));
        Assert.Contains(feedbackMessage, feedbackStrip.TextContent, StringComparison.Ordinal);
    }
}