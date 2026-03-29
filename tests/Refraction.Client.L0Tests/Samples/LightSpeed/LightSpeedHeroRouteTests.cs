using System;

using AngleSharp.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;

using LightSpeedIndexPage = MississippiSamples.LightSpeed.Client.Pages.Index;


namespace Mississippi.Refraction.Client.L0Tests.Samples.LightSpeed;

/// <summary>
///     Verifies the LightSpeed base-only Refraction hero route.
/// </summary>
public sealed class LightSpeedHeroRouteTests : BunitContext
{
    private IRenderedComponent<LightSpeedIndexPage> RenderHeroRoute()
    {
        Services.AddLogging();
        Services.AddRefraction();
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<LightSpeedIndexPage>();
    }

    private static IElement FindFeedbackBanner(
        IRenderedComponent<LightSpeedIndexPage> cut
    ) =>
        cut.Find(".rf-telemetry-strip.ls-workbench__feedback[data-testid='feedback-banner']");

    /// <summary>
    ///     LightSpeed applies the selected action and exposes the stable post-action state.
    /// </summary>
    [Fact]
    public void LightSpeedAppliesActionForReadyWorkItem()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();
        cut.Find("[data-testid='queue-select-OPS-1047']").Click();

        // Act
        cut.Find("[data-testid='apply-action']").Click();

        // Assert
        IElement feedbackBanner = FindFeedbackBanner(cut);
        Assert.Equal("Actioned", cut.Find("[data-testid='selected-stage']").TextContent.Trim());
        Assert.Equal("Response package released", cut.Find("[data-testid='selected-checkpoint']").TextContent.Trim());
        Assert.Equal("success", feedbackBanner.GetAttribute("data-state"));
        Assert.Contains(
            "OPS-1047 action completed",
            feedbackBanner.TextContent,
            StringComparison.Ordinal);
        Assert.True(cut.Find("[data-testid='apply-action']").HasAttribute("disabled"));
    }

    /// <summary>
    ///     LightSpeed cancels review validation state and restores the persisted work item values when the dialog is reopened.
    /// </summary>
    [Fact]
    public void LightSpeedCancelingReviewClearsValidationStateWhenDialogReopens()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();
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
    ///     LightSpeed renders the base-only workbench shell with an initial selection.
    /// </summary>
    [Fact]
    public void LightSpeedRendersBaseOnlyWorkbenchShellWithInitialSelection()
    {
        // Act
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();

        // Assert
        Assert.Equal("horizon", cut.Find(".rf-root").GetAttribute("data-rf-brand"));
        Assert.NotNull(cut.Find("[data-testid='status-summary-bar']"));
        Assert.Equal(2, cut.FindAll(".rf-surface-panel").Count);
        Assert.Equal(2, cut.FindAll(".rf-command-button").Count);
        Assert.Equal(5, cut.FindAll(".rf-status-badge").Count);
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='review-open']"));
        Assert.NotNull(cut.Find(".rf-command-button[data-testid='apply-action']"));
        Assert.False(cut.Find("[data-testid='review-open']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='apply-action']").HasAttribute("disabled"));
        Assert.NotNull(cut.Find(".rf-status-badge[data-testid='selected-stage']"));
        Assert.Empty(cut.FindAll(".rf-telemetry-strip"));
        Assert.Empty(cut.FindAll(".rf-pane"));
        Assert.Equal("OPS-1042", cut.Find("[data-testid='selected-id']").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("[data-testid^='queue-select-']"));
    }

    /// <summary>
    ///     LightSpeed renders the increment-5 replacement command buttons inside the base-only review dialog actions.
    /// </summary>
    [Fact]
    public void LightSpeedRendersReplacementLeafControlsInsideReviewDialog()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();

        // Act
        cut.Find("[data-testid='review-open']").Click();

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

    /// <summary>
    ///     LightSpeed saves non-dispatch review decisions with the expected pending-review checkpoint message.
    /// </summary>
    /// <param name="disposition">The reviewed disposition to persist.</param>
    /// <param name="reviewNotes">The supporting review notes for the updated plan.</param>
    /// <param name="expectedCheckpoint">The checkpoint that should be rendered after save.</param>
    [Theory]
    [InlineData(
        "Hold",
        "Permit variance still pending district approval, keep the reroute paused.",
        "Held for manual review")]
    [InlineData(
        "Investigate",
        "Escalate the carrier handoff for a targeted compliance review before dispatch.",
        "Escalated for further investigation")]
    public void LightSpeedSavesNonDispatchDispositionWithExpectedPendingReviewCheckpoint(
        string disposition,
        string reviewNotes,
        string expectedCheckpoint
    )
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();
        cut.Find("[data-testid='review-open']").Click();

        // Act
        cut.Find("[data-testid='review-disposition']").Change(disposition);
        cut.Find("#ls-review-summary")
            .Input("Extend the verification window so the operations lead can confirm the next safe move.");
        cut.Find("[data-testid='review-notes']").Input(reviewNotes);
        cut.Find("[data-testid='review-save']").Click();

        // Assert
        IElement feedbackBanner = FindFeedbackBanner(cut);
        Assert.Equal(disposition, cut.Find("[data-testid='selected-disposition']").TextContent.Trim());
        Assert.Equal("Pending review", cut.Find("[data-testid='selected-stage']").TextContent.Trim());
        Assert.Equal(expectedCheckpoint, cut.Find("[data-testid='selected-checkpoint']").TextContent.Trim());
        Assert.Equal("success", feedbackBanner.GetAttribute("data-state"));
        Assert.Contains(
            "OPS-1042 updated.",
            feedbackBanner.TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     LightSpeed clears selection and shows the empty state when combined filters remove every visible work item.
    /// </summary>
    [Fact]
    public void LightSpeedShowsEmptyStateWhenFiltersRemoveAllVisibleWorkItems()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();
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

    /// <summary>
    ///     LightSpeed switches brands live and keeps the selected row aligned with filtered results.
    /// </summary>
    [Fact]
    public void LightSpeedSwitchesBrandAndFiltersQueueSelection()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();

        // Act
        cut.Find("[data-testid='brand-signal']").Click();
        cut.Find("#ls-queue-search").Input("Litware");

        // Assert
        Assert.Equal("signal", cut.Find(".rf-root").GetAttribute("data-rf-brand"));
        Assert.Equal("OPS-1047", cut.Find("[data-testid='selected-id']").TextContent.Trim());
        Assert.Single(cut.FindAll("[data-testid^='queue-select-']"));
    }

    /// <summary>
    ///     LightSpeed validates the review dialog and persists a corrected edit.
    /// </summary>
    [Fact]
    public void LightSpeedValidatesAndSavesReviewEdits()
    {
        // Arrange
        using IRenderedComponent<LightSpeedIndexPage> cut = RenderHeroRoute();

        // Act
        cut.Find("[data-testid='review-open']").Click();
        cut.Find("#ls-review-analyst").Input(string.Empty);
        cut.Find("#ls-review-summary").Input("Too short");
        cut.Find("[data-testid='review-disposition']").Change("Hold");
        cut.Find("[data-testid='review-notes']").Input(string.Empty);
        cut.Find("[data-testid='review-save']").Click();

        // Assert
        IElement validationSummary = cut.Find("[data-testid='validation-summary']");
        Assert.Contains("Assigned analyst is required.", validationSummary.TextContent, StringComparison.Ordinal);
        Assert.Contains(
            "Response summary must contain at least 18 characters.",
            validationSummary.TextContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "Review notes are required when the work item is held.",
            validationSummary.TextContent,
            StringComparison.Ordinal);
        Assert.Equal("warning", FindFeedbackBanner(cut).GetAttribute("data-state"));
        Assert.Contains(
            "Resolve the validation errors before saving the review.",
            FindFeedbackBanner(cut).TextContent,
            StringComparison.Ordinal);

        // Act
        cut.Find("#ls-review-analyst").Input("Iris Valdez");
        cut.Find("#ls-review-summary")
            .Input("Dispatch the compliance-approved replacement unit before the evening cut-off.");
        cut.Find("[data-testid='review-disposition']").Change("Dispatch");
        cut.Find("[data-testid='review-notes']")
            .Input("Compliance check cleared and the field team has the release packet.");
        cut.Find("[data-testid='review-save']").Click();

        // Assert
        IElement feedbackBanner = FindFeedbackBanner(cut);
        Assert.Empty(cut.FindAll("[data-testid='validation-summary']"));
        Assert.Equal("Iris Valdez", cut.Find("[data-testid='selected-analyst']").TextContent.Trim());
        Assert.Equal("Dispatch", cut.Find("[data-testid='selected-disposition']").TextContent.Trim());
        Assert.Equal("Ready", cut.Find("[data-testid='selected-stage']").TextContent.Trim());
        Assert.Equal("success", feedbackBanner.GetAttribute("data-state"));
        Assert.Contains(
            "OPS-1042 updated.",
            feedbackBanner.TextContent,
            StringComparison.Ordinal);
    }
}