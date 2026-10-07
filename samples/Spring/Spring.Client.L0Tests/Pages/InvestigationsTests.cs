using System;
using System.Collections.Immutable;
using System.Globalization;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.FlaggedTransactions.Dtos;
using MississippiSamples.Spring.Client.L0Tests.Components.Templates;
using MississippiSamples.Spring.Client.Pages;

using Moq;


namespace MississippiSamples.Spring.Client.L0Tests.Pages;

/// <summary>
///     Verifies mutually exclusive investigation queue outcomes using real projection reducers.
/// </summary>
public sealed class InvestigationsTests : BunitContext
{
    private const string GlobalEntityId = "global";

    /// <summary>
    ///     Creates a missing, loaded-empty or populated queue through the production reducer.
    /// </summary>
    /// <param name="entryCount">Minus one for missing data, zero for empty, or one for a populated queue.</param>
    /// <returns>The projection feature state.</returns>
    private static ProjectionsFeatureState CreateQueueState(
        int entryCount
    )
    {
        if (entryCount < 0)
        {
            return new();
        }

        DateTimeOffset deposited = new(2026, 1, 2, 12, 45, 0, TimeSpan.FromHours(2));
        ImmutableArray<FlaggedTransactionDto> entries = entryCount == 0
            ? []
            :
            [
                new(
                    "flagged-account",
                    10001m,
                    OriginalTimestamp: deposited,
                    FlaggedTimestamp: deposited.AddMinutes(1),
                    Sequence: 1),
            ];
        return ProjectionsReducer.ReduceLoaded(
            new(),
            new ProjectionLoadedAction<FlaggedTransactionsProjectionDto>(
                GlobalEntityId,
                new(Entries: entries, CurrentSequence: entryCount),
                9));
    }

    /// <summary>
    ///     Supplies reducer-produced state without running subscription effects.
    /// </summary>
    /// <param name="state">The queue state to render.</param>
    /// <returns>The registered store.</returns>
    private IInletStore RegisterStore(
        ProjectionsFeatureState state
    )
    {
        Mock<IInletStore> store = new(MockBehavior.Strict);
        store.Setup(current => current.GetState<ProjectionsFeatureState>()).Returns(state);
        store.Setup(current => current.GetState<SignalRConnectionState>()).Returns(new SignalRConnectionState());
        store.Setup(current => current.Subscribe(It.IsAny<Action>())).Returns(() => new EmptyStoreEventSubscription());
        store.Setup(current => current.Dispatch(It.IsAny<IAction>()));
        store.Setup(current => current.Dispose());
        Services.AddSingleton<IStore>(store.Object);
        Services.AddSingleton(store.Object);
        return store.Object;
    }

    /// <summary>Verify that a failed read shows only the error outcome, even when earlier queue data remains cached.</summary>
    /// <param name="cachedEntries">The number of cached entries, or minus one for no prior data.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void FailedReadDoesNotShowHealthyQueueOutcomes(
        int cachedEntries
    )
    {
        ProjectionsFeatureState prior = CreateQueueState(cachedEntries);
        InvalidOperationException error = new("The protected queue read failed.");
        ProjectionsFeatureState failed = ProjectionsReducer.ReduceError(
            prior,
            new ProjectionErrorAction<FlaggedTransactionsProjectionDto>(GlobalEntityId, error));
        IInletStore store = RegisterStore(failed);
        using IRenderedComponent<Investigations> cut = Render<Investigations>();
        Assert.Contains(
            "The queue could not be read",
            Assert.Single(cut.FindAll("[role='alert']")).TextContent,
            StringComparison.Ordinal);
        Assert.Contains(error.Message, cut.Markup, StringComparison.Ordinal);
        Assert.Equal("Read error details", cut.Find(".spring-queue-error details summary").TextContent);
        Assert.False(cut.Find(".spring-queue-error details").HasAttribute("open"));
        Assert.DoesNotContain("No queue data received yet", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("No flagged deposits", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Projection version", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".spring-queue-empty, .spring-queue-scroll, table, [role='status']"));
        ProjectionsFeatureState retained = store.GetState<ProjectionsFeatureState>();
        Assert.Same(
            prior.GetProjection<FlaggedTransactionsProjectionDto>(GlobalEntityId),
            retained.GetProjection<FlaggedTransactionsProjectionDto>(GlobalEntityId));
        Assert.Equal(
            prior.GetProjectionVersion<FlaggedTransactionsProjectionDto>(GlobalEntityId),
            retained.GetProjectionVersion<FlaggedTransactionsProjectionDto>(GlobalEntityId));
        Assert.Same(error, retained.GetProjectionError<FlaggedTransactionsProjectionDto>(GlobalEntityId));
    }

    /// <summary>Verify that a successfully loaded empty queue is distinct from a missing or failed read.</summary>
    [Fact]
    public void LoadedEmptyQueueShowsNoFlaggedDeposits()
    {
        RegisterStore(CreateQueueState(0));
        using IRenderedComponent<Investigations> cut = Render<Investigations>();
        Assert.Contains(
            "No flagged deposits",
            Assert.Single(cut.FindAll(".spring-queue-empty")).TextContent,
            StringComparison.Ordinal);
        Assert.Contains("The loaded queue is empty.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Projection version 9", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("No queue data received yet", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role='alert'], [role='status'], table"));
    }

    /// <summary>
    ///     Verify that a successful populated read retains its accessible region, actual rows, version and UTC
    ///     timestamps.
    /// </summary>
    [Fact]
    public void LoadedQueueShowsAccessibleRowsAndUtcTimes()
    {
        RegisterStore(CreateQueueState(1));
        using IRenderedComponent<Investigations> cut = Render<Investigations>();
        Assert.Equal("0", cut.Find("[role='region'][aria-label='Flagged deposit entries']").GetAttribute("tabindex"));
        string row = Assert.Single(cut.FindAll("tbody tr")).TextContent;
        Assert.Contains("flagged-account", row, StringComparison.Ordinal);
        Assert.Contains("£10,001.00", row, StringComparison.Ordinal);
        Assert.Contains("2026-01-02 10:45:00", row, StringComparison.Ordinal);
        Assert.Contains("2026-01-02 10:46:00", row, StringComparison.Ordinal);
        Assert.Contains("Times are shown in UTC.", cut.Find("caption").TextContent, StringComparison.Ordinal);
        Assert.Equal(4, cut.FindAll("th[scope='col']").Count);
        Assert.Contains("Projection version 9", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role='alert'], [role='status'], .spring-queue-empty"));
    }

    /// <summary>Verify that loading suppresses empty and populated outcomes without deleting cached data.</summary>
    /// <param name="cachedEntries">The number of cached entries, or minus one for no prior data.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void LoadingShowsOnlyPendingQueueOutcome(
        int cachedEntries
    )
    {
        ProjectionsFeatureState state = ProjectionsReducer.ReduceLoading(
            CreateQueueState(cachedEntries),
            new ProjectionLoadingAction<FlaggedTransactionsProjectionDto>(GlobalEntityId));
        RegisterStore(state);
        using IRenderedComponent<Investigations> cut = Render<Investigations>();
        Assert.Equal("Loading the investigation queue…", Assert.Single(cut.FindAll("[role='status']")).TextContent);
        Assert.Empty(cut.FindAll("[role='alert'], .spring-queue-empty, table"));
        Assert.DoesNotContain("No queue data received yet", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verify that missing queue data retains its distinct prerequisite and live-update guidance.</summary>
    [Fact]
    public void MissingDataShowsJourneyGuidance()
    {
        RegisterStore(CreateQueueState(-1));
        using IRenderedComponent<Investigations> cut = Render<Investigations>();
        Assert.Contains(
            "No queue data received yet. Complete the journey above and wait for a live update.",
            cut.Markup,
            StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role='alert'], [role='status'], .spring-queue-empty, table"));
        Assert.DoesNotContain("Projection version", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verify fixed GBP formatting and Gregorian UTC timestamps under different cultures.</summary>
    /// <param name="cultureName">A culture with different number separators, time separators or calendar.</param>
    [Theory]
    [InlineData("de-DE")]
    [InlineData("fi-FI")]
    [InlineData("th-TH")]
    public void QueueFormattingUsesInvariantGbpAndUtc(
        string cultureName
    )
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            RegisterStore(CreateQueueState(1));
            using IRenderedComponent<Investigations> cut = Render<Investigations>();
            string row = Assert.Single(cut.FindAll("tbody tr")).TextContent;
            Assert.Contains("£10,001.00", row, StringComparison.Ordinal);
            Assert.Contains("2026-01-02 10:45:00", row, StringComparison.Ordinal);
            Assert.Contains("2026-01-02 10:46:00", row, StringComparison.Ordinal);
            Assert.Contains("Times are shown in UTC.", cut.Find("caption").TextContent, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}