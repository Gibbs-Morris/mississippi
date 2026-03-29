using System;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;
using Mississippi.Refraction.Client.StateManagement.Infrastructure;
using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

using LightSpeedIndexPage = MississippiSamples.LightSpeed.Client.Pages.Index;
using LightSpeedReservoirWorkbenchPage = MississippiSamples.LightSpeed.Client.Pages.ReservoirOperationsWorkbenchPage;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed;

/// <summary>
///     Verifies the LightSpeed Reservoir-explicit parity route.
/// </summary>
public sealed class LightSpeedReservoirWorkbenchRouteTests : BunitContext
{
    private bool servicesConfigured;

    private sealed record EmptyWorkbenchSnapshot(
        int EmptyStateCount,
        string QueueEmptyMessage,
        string DetailEmptyMessage,
        int VisibleQueueItems,
        bool IsReviewDisabled,
        bool IsApplyActionDisabled
    );

    private sealed record FeedbackStripSnapshot(
        string Message,
        string State
    );

    private static void AssertReplacementLeafControls<TComponent>(
        IRenderedComponent<TComponent> cut,
        bool isReviewDialogOpen
    )
        where TComponent : IComponent
    {
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='review-open']"));
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='apply-action']"));
        Assert.NotNull(cut.Find(".rf-status-badge[data-testid='selected-stage']"));
        Assert.Equal(isReviewDialogOpen ? 4 : 2, cut.FindAll(".rf-command-button").Count);
        Assert.Equal(5, cut.FindAll(".rf-status-badge").Count);
        if (!isReviewDialogOpen)
        {
            return;
        }

        Assert.NotNull(cut.Find("[data-testid='review-dialog']"));
        Assert.Collection(
            cut.FindAll(".ls-review-dialog__actions .rf-command-button"),
            button => Assert.Equal("Cancel", button.TextContent.Trim()),
            button => Assert.Equal("Save review", button.TextContent.Trim()));
        Assert.NotNull(cut.Find(".ls-review-dialog__actions .rf-command-button[data-testid='review-save']"));
    }

    private static WorkbenchSnapshot CaptureSnapshot<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent =>
        new(
            cut.Find(".rf-root").GetAttribute("data-rf-brand") ?? string.Empty,
            cut.Find("[data-testid='selected-analyst']").TextContent.Trim(),
            cut.Find("[data-testid='selected-checkpoint']").TextContent.Trim(),
            cut.Find("[data-testid='selected-disposition']").TextContent.Trim(),
            cut.Find("[data-testid='selected-id']").TextContent.Trim(),
            cut.Find("[data-testid='selected-stage']").TextContent.Trim(),
            cut.FindAll("[data-testid^='queue-select-']").Count);

    private static string CaptureValidationSummary<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        IElement validationSummary = cut.Find("[data-testid='validation-summary']");
        return validationSummary.TextContent.Trim();
    }

    private static EmptyWorkbenchSnapshot CaptureEmptyWorkbenchSnapshot<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        cut.Find(".rf-empty-state.ls-workbench__empty-state[data-testid='queue-empty-state']");
        cut.Find(".rf-empty-state.ls-workbench__empty-state[data-testid='detail-empty-state']");

        return new(
            cut.FindAll(".rf-empty-state").Count,
            cut.Find("[data-testid='queue-empty-state']").TextContent.Trim(),
            cut.Find("[data-testid='detail-empty-state']").TextContent.Trim(),
            cut.FindAll("[data-testid^='queue-select-']").Count,
            cut.Find("[data-testid='review-open']").HasAttribute("disabled"),
            cut.Find("[data-testid='apply-action']").HasAttribute("disabled"));
    }

    private static FeedbackStripSnapshot CaptureFeedbackStripSnapshot<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent =>
        new(
            cut.Find(".rf-telemetry-strip.ls-workbench__feedback[data-testid='feedback-banner']").TextContent.Trim(),
            cut.Find(".rf-telemetry-strip.ls-workbench__feedback[data-testid='feedback-banner']").GetAttribute("data-state") ?? string.Empty);

    private static void DriveParityBrandAndFilterFlow<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        cut.Find("[data-testid='brand-signal']").Click();
        cut.Find("#ls-queue-search").Input("Litware");
    }

    private static void DriveParitySaveFlow<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        cut.Find("#ls-review-analyst").Input("Iris Valdez");
        cut.Find("#ls-review-summary")
            .Input("Dispatch the compliance-approved replacement unit before the evening cut-off.");
        cut.Find("[data-testid='review-disposition']").Change("Dispatch");
        cut.Find("[data-testid='review-notes']")
            .Input("Compliance check cleared and the field team has the release packet.");
        cut.Find("[data-testid='review-save']").Click();
    }

    private static void DriveParityValidationFlow<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        cut.Find("[data-testid='review-open']").Click();
        cut.Find("#ls-review-analyst").Input(string.Empty);
        cut.Find("#ls-review-summary").Input("Too short");
        cut.Find("[data-testid='review-disposition']").Change("Hold");
        cut.Find("[data-testid='review-notes']").Input(string.Empty);
        cut.Find("[data-testid='review-save']").Click();
    }

    private static void DriveParityEmptyStateFlow<TComponent>(
        IRenderedComponent<TComponent> cut
    )
        where TComponent : IComponent
    {
        cut.Find("#ls-queue-search").Input("Northwind");
        cut.Find("[data-testid='stage-filter']").Change("Ready");
    }

    private void ConfigureServices()
    {
        if (servicesConfigured)
        {
            return;
        }

        Services.AddLogging();
        Services.AddRefraction();
        Services.AddRefractionReservoirPages();
        Services.AddReservoir().AddReservoirOperationsWorkbenchFeature();
        JSInterop.Mode = JSRuntimeMode.Loose;
        servicesConfigured = true;
    }

    private IRenderedComponent<LightSpeedIndexPage> RenderBaseOnlyRoute()
    {
        ConfigureServices();
        return Render<LightSpeedIndexPage>();
    }

    private IRenderedComponent<LightSpeedReservoirWorkbenchPage> RenderReservoirWorkbenchRoute()
    {
        ConfigureServices();
        return Render<LightSpeedReservoirWorkbenchPage>();
    }

    private sealed record WorkbenchSnapshot(
        string Brand,
        string SelectedAnalyst,
        string SelectedCheckpoint,
        string SelectedDisposition,
        string SelectedId,
        string SelectedStage,
        int VisibleQueueItems
    );

    /// <summary>
    ///     The Reservoir parity route clears validation state and restores persisted review values when the dialog is
    ///     reopened.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteCancelingReviewClearsValidationStateWhenDialogReopens()
    {
        // Arrange
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> cut = RenderReservoirWorkbenchRoute();
        cut.Find("[data-testid='review-open']").Click();
        cut.Find("#ls-review-analyst").Input(string.Empty);
        cut.Find("#ls-review-summary").Input("Too short");
        cut.Find("[data-testid='review-disposition']").Change("Hold");
        cut.Find("[data-testid='review-notes']").Input(string.Empty);
        cut.Find("[data-testid='review-save']").Click();

        // Act
        cut.Find("button[aria-label='Close review dialog']").Click();
        cut.Find("[data-testid='review-open']").Click();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='validation-summary']"));
        Assert.Equal("Maya Chen", cut.Find("#ls-review-analyst").GetAttribute("value"));
        Assert.Equal("Investigate", cut.Find("[data-testid='review-disposition']").GetAttribute("value"));
        Assert.Equal(
            "Confirm the refrigeration transfer window before releasing the dispatch order.",
            cut.Find("#ls-review-summary").GetAttribute("value"));
        Assert.Equal(
            "Regional desk requested one more compliance check before the replacement unit leaves the depot.",
            cut.Find("[data-testid='review-notes']").GetAttribute("value"));
    }

    /// <summary>
    ///     The Reservoir parity route matches the base route for brand switching and queue filtering behavior.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteMatchesBaseRouteForBrandSwitchAndFiltering()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> baseRoute = RenderBaseOnlyRoute();
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> reservoirRoute = RenderReservoirWorkbenchRoute();

        // Act
        DriveParityBrandAndFilterFlow(baseRoute);
        DriveParityBrandAndFilterFlow(reservoirRoute);

        // Assert
        Assert.Equal(CaptureSnapshot(baseRoute), CaptureSnapshot(reservoirRoute));
    }

    /// <summary>
    ///     The Reservoir parity route matches the base route for the increment-6 empty-state filtering experience.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteMatchesBaseRouteForEmptyStateFiltering()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> baseRoute = RenderBaseOnlyRoute();
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> reservoirRoute = RenderReservoirWorkbenchRoute();

        // Act
        DriveParityEmptyStateFlow(baseRoute);
        DriveParityEmptyStateFlow(reservoirRoute);

        // Assert
        Assert.Equal(CaptureEmptyWorkbenchSnapshot(baseRoute), CaptureEmptyWorkbenchSnapshot(reservoirRoute));

        // Act
        baseRoute.Find("[data-testid='stage-filter']").Change("Pending review");
        reservoirRoute.Find("[data-testid='stage-filter']").Change("Pending review");

        // Assert
        Assert.Equal(CaptureSnapshot(baseRoute), CaptureSnapshot(reservoirRoute));
    }

    /// <summary>
    ///     The Reservoir parity route matches the base route for the increment-5 review-dialog leaf controls.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteMatchesBaseRouteForReviewDialogLeafControls()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> baseRoute = RenderBaseOnlyRoute();
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> reservoirRoute = RenderReservoirWorkbenchRoute();

        // Act
        baseRoute.Find("[data-testid='review-open']").Click();
        reservoirRoute.Find("[data-testid='review-open']").Click();

        // Assert
        Assert.Equal(CaptureSnapshot(baseRoute), CaptureSnapshot(reservoirRoute));
        AssertReplacementLeafControls(baseRoute, true);
        AssertReplacementLeafControls(reservoirRoute, true);
    }

    /// <summary>
    ///     The Reservoir parity route matches the base route for validation, save, and ready-action flows.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteMatchesBaseRouteForValidationSaveAndAction()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> baseRoute = RenderBaseOnlyRoute();
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> reservoirRoute = RenderReservoirWorkbenchRoute();

        // Act
        DriveParityValidationFlow(baseRoute);
        DriveParityValidationFlow(reservoirRoute);

        // Assert
        Assert.Equal(CaptureValidationSummary(baseRoute), CaptureValidationSummary(reservoirRoute));
        Assert.Equal(CaptureFeedbackStripSnapshot(baseRoute), CaptureFeedbackStripSnapshot(reservoirRoute));
        Assert.Equal("warning", CaptureFeedbackStripSnapshot(baseRoute).State);

        // Act
        DriveParitySaveFlow(baseRoute);
        DriveParitySaveFlow(reservoirRoute);
        baseRoute.Find("[data-testid='queue-select-OPS-1047']").Click();
        reservoirRoute.Find("[data-testid='queue-select-OPS-1047']").Click();
        baseRoute.Find("[data-testid='apply-action']").Click();
        reservoirRoute.Find("[data-testid='apply-action']").Click();

        // Assert
        Assert.Equal(CaptureSnapshot(baseRoute), CaptureSnapshot(reservoirRoute));
        Assert.Equal(CaptureFeedbackStripSnapshot(baseRoute), CaptureFeedbackStripSnapshot(reservoirRoute));
        Assert.Equal("success", CaptureFeedbackStripSnapshot(baseRoute).State);
        Assert.Contains(
            "OPS-1047 action completed",
            baseRoute.Find("[data-testid='feedback-banner']").TextContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "OPS-1047 action completed",
            reservoirRoute.Find("[data-testid='feedback-banner']").TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     The Reservoir parity route renders the same initial workbench surface as the base-only route.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteMatchesBaseRouteOnInitialRender()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> baseRoute = RenderBaseOnlyRoute();
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> reservoirRoute = RenderReservoirWorkbenchRoute();

        // Act
        WorkbenchSnapshot baseSnapshot = CaptureSnapshot(baseRoute);
        WorkbenchSnapshot reservoirSnapshot = CaptureSnapshot(reservoirRoute);

        // Assert
        Assert.Equal(baseRoute.Find("h1").TextContent.Trim(), reservoirRoute.Find("h1").TextContent.Trim());
        Assert.Equal(baseSnapshot, reservoirSnapshot);
        AssertReplacementLeafControls(baseRoute, false);
        AssertReplacementLeafControls(reservoirRoute, false);
    }

    /// <summary>
    ///     The Reservoir parity route clears selection and shows the empty state when combined filters remove every visible
    ///     work item.
    /// </summary>
    [Fact]
    public void ReservoirWorkbenchRouteShowsEmptyStateWhenFiltersRemoveAllVisibleWorkItems()
    {
        // Arrange
        using IRenderedComponent<LightSpeedReservoirWorkbenchPage> cut = RenderReservoirWorkbenchRoute();
        cut.Find("#ls-queue-search").Input("Northwind");

        // Act
        cut.Find("[data-testid='stage-filter']").Change("Ready");

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

        // Act
        cut.Find("[data-testid='stage-filter']").Change("Pending review");

        // Assert
        Assert.Equal("OPS-1042", cut.Find("[data-testid='selected-id']").TextContent.Trim());
    }
}