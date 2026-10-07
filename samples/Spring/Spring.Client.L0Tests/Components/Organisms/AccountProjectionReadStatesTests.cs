using System;
using System.Collections.Immutable;

using AngleSharp.Dom;

using Bunit;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;

using MississippiSamples.Spring.Client.Components.Organisms;
using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.BankAccountLedger.Dtos;
using MississippiSamples.Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Verifies exclusive account read outcomes using the real projection reducers.
/// </summary>
public sealed class AccountProjectionReadStatesTests : BunitContext
{
    private const string EntityId = "observed-account";

    private const string SagaId = "observed-transfer";

    private static void AssertRetained<T>(
        ProjectionsFeatureState prior,
        ProjectionsFeatureState failed,
        Exception error,
        string entityId = EntityId
    )
        where T : class
    {
        Assert.Same(prior.GetProjection<T>(entityId), failed.GetProjection<T>(entityId));
        Assert.Equal(prior.GetProjectionVersion<T>(entityId), failed.GetProjectionVersion<T>(entityId));
        Assert.Same(error, failed.GetProjectionError<T>(entityId));
        Assert.False(failed.GetEntry<T>(entityId)?.IsLoading);
    }

    private static BankAccountLedgerProjectionDto? CreateLedger(
        int entryCount
    )
    {
        if (entryCount < 0)
        {
            return null;
        }

        ImmutableArray<LedgerEntryDto> entries = entryCount == 0 ? [] : [new(25m, LedgerEntryTypeDto.Deposit, 7)];
        return new(7, entries);
    }

    private static ProjectionsFeatureState CreateState<T>(
        T? projection,
        string entityId = EntityId
    )
        where T : class =>
        projection is null
            ? new()
            : ProjectionsReducer.ReduceLoaded(new(), new ProjectionLoadedAction<T>(entityId, projection, 9));

    private IRenderedComponent<AccountOperationsSection> RenderBalance(
        ProjectionsFeatureState state
    ) =>
        Render<AccountOperationsSection>(parameters => parameters.Add(component => component.InputIdPrefix, "account-a")
            .Add(component => component.PanelLabel, "Account A")
            .Add(component => component.SelectedEntityId, EntityId)
            .Add(
                component => component.BalanceProjection,
                state.GetProjection<BankAccountBalanceProjectionDto>(EntityId))
            .Add(
                component => component.BalanceVersion,
                state.GetProjectionVersion<BankAccountBalanceProjectionDto>(EntityId))
            .Add(
                component => component.IsBalanceLoading,
                state.GetEntry<BankAccountBalanceProjectionDto>(EntityId)?.IsLoading ?? false)
            .Add(
                component => component.ErrorMessage,
                state.GetProjectionError<BankAccountBalanceProjectionDto>(EntityId)?.Message));

    private IRenderedComponent<AccountOperationsSection> RenderLedger(
        ProjectionsFeatureState state
    ) =>
        Render<AccountOperationsSection>(parameters => parameters.Add(component => component.InputIdPrefix, "account-a")
            .Add(component => component.PanelLabel, "Account A")
            .Add(component => component.SelectedEntityId, EntityId)
            .Add(component => component.LedgerProjection, state.GetProjection<BankAccountLedgerProjectionDto>(EntityId))
            .Add(
                component => component.LedgerVersion,
                state.GetProjectionVersion<BankAccountLedgerProjectionDto>(EntityId))
            .Add(
                component => component.IsLedgerLoading,
                state.GetEntry<BankAccountLedgerProjectionDto>(EntityId)?.IsLoading ?? false)
            .Add(
                component => component.LedgerError,
                state.GetProjectionError<BankAccountLedgerProjectionDto>(EntityId)?.Message));

    /// <summary>Verify that a failed cold or cached balance read cannot display a waiting state or an unqualified live value.</summary>
    /// <param name="cached">Whether a prior successful balance is retained.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedBalanceReadHidesMissingAndHealthyOutcomes(
        bool cached
    )
    {
        BankAccountBalanceProjectionDto? projection = cached ? new(500m, "Ada", true) : null;
        ProjectionsFeatureState prior = CreateState(projection);
        InvalidOperationException error = new("The balance read failed <unsafe>.");
        ProjectionsFeatureState failed = ProjectionsReducer.ReduceError(
            prior,
            new ProjectionErrorAction<BankAccountBalanceProjectionDto>(EntityId, error));
        using IRenderedComponent<AccountOperationsSection> cut = RenderBalance(failed);
        IElement balance = cut.Find(".spring-balance");
        Assert.DoesNotContain("No account data received", balance.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Loading the live account", balance.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Balance projection version", balance.TextContent, StringComparison.Ordinal);
        Assert.Empty(balance.QuerySelectorAll("output[data-spring-balance], .spring-holder"));
        Assert.Contains("The account could not be read", balance.TextContent, StringComparison.Ordinal);
        Assert.Contains(
            error.Message,
            Assert.Single(balance.QuerySelectorAll("[role='alert']")).TextContent,
            StringComparison.Ordinal);
        Assert.Equal("Read error details", balance.QuerySelector("details summary")?.TextContent);
        Assert.False(balance.QuerySelector("details")?.HasAttribute("open"));
        Assert.Empty(balance.QuerySelectorAll("unsafe"));
        AssertRetained<BankAccountBalanceProjectionDto>(prior, failed, error);
    }

    /// <summary>Verify that a ledger error cannot also claim loading, missing, empty or successfully read transactions.</summary>
    /// <param name="cachedEntries">Minus one for missing data, zero for empty, or one for a prior transaction.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void FailedLedgerReadHidesMissingEmptyAndHealthyOutcomes(
        int cachedEntries
    )
    {
        ProjectionsFeatureState prior = CreateState(CreateLedger(cachedEntries));
        InvalidOperationException error = new("The ledger read failed <unsafe>.");
        ProjectionsFeatureState failed = ProjectionsReducer.ReduceError(
            prior,
            new ProjectionErrorAction<BankAccountLedgerProjectionDto>(EntityId, error));
        using IRenderedComponent<AccountOperationsSection> cut = RenderLedger(failed);
        IElement ledger = cut.Find(".spring-ledger");
        Assert.DoesNotContain("No ledger data received", ledger.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("No transactions yet", ledger.TextContent, StringComparison.Ordinal);
        Assert.Empty(ledger.QuerySelectorAll("table, [role='status']"));
        Assert.Contains("The ledger could not be read", ledger.TextContent, StringComparison.Ordinal);
        Assert.Contains(
            error.Message,
            Assert.Single(ledger.QuerySelectorAll("[role='alert']")).TextContent,
            StringComparison.Ordinal);
        Assert.Equal("Read error details", ledger.QuerySelector("details summary")?.TextContent);
        Assert.False(ledger.QuerySelector("details")?.HasAttribute("open"));
        Assert.Empty(ledger.QuerySelectorAll("unsafe"));
        AssertRetained<BankAccountLedgerProjectionDto>(prior, failed, error);
    }

    /// <summary>Verify that a status-read failure does not establish a pending or final saga outcome, even with retained data.</summary>
    /// <param name="cachedPhase">The prior observed phase, or null before any status has arrived.</param>
    [Theory]
    [InlineData(null)]
    [InlineData(SagaPhaseDto.NotStarted)]
    [InlineData(SagaPhaseDto.Running)]
    [InlineData(SagaPhaseDto.Completed)]
    [InlineData(SagaPhaseDto.Compensated)]
    [InlineData(SagaPhaseDto.Compensating)]
    [InlineData(SagaPhaseDto.Failed)]
    public void FailedTransferReadHidesWaitingAndCachedSagaOutcomes(
        SagaPhaseDto? cachedPhase
    )
    {
        MoneyTransferStatusProjectionDto? projection = cachedPhase is null
            ? null
            : new(null, null, null, 0, cachedPhase.Value, new(2026, 1, 2, 10, 15, 0, TimeSpan.Zero));
        ProjectionsFeatureState prior = CreateState(projection, SagaId);
        InvalidOperationException error = new("The transfer status read failed <unsafe>.");
        ProjectionsFeatureState failed = ProjectionsReducer.ReduceError(
            prior,
            new ProjectionErrorAction<MoneyTransferStatusProjectionDto>(SagaId, error));
        using IRenderedComponent<AccountOperationsSection> cut = Render<AccountOperationsSection>(parameters =>
            parameters.Add(component => component.InputIdPrefix, "account-a")
                .Add(component => component.TransferSagaId, SagaId)
                .Add(
                    component => component.TransferStatusProjection,
                    failed.GetProjection<MoneyTransferStatusProjectionDto>(SagaId))
                .Add(
                    component => component.TransferReadError,
                    failed.GetProjectionError<MoneyTransferStatusProjectionDto>(SagaId)?.Message));
        IElement panel = cut.Find("#account-a-transfer-panel");
        Assert.DoesNotContain("Waiting for transfer status", panel.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Start a transfer to see saga status", panel.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Phase:", panel.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Last completed step:", panel.TextContent, StringComparison.Ordinal);
        Assert.Contains("Transfer saga ID: " + SagaId, panel.TextContent, StringComparison.Ordinal);
        Assert.Contains("Transfer status could not be read", panel.TextContent, StringComparison.Ordinal);
        Assert.Contains(
            error.Message,
            Assert.Single(panel.QuerySelectorAll("[role='alert']")).TextContent,
            StringComparison.Ordinal);
        Assert.Equal("error", cut.Find("#account-a-transfer-status").GetAttribute("data-state"));
        Assert.Equal("Read error details", panel.QuerySelector("details summary")?.TextContent);
        Assert.False(panel.QuerySelector("details")?.HasAttribute("open"));
        Assert.Empty(panel.QuerySelectorAll("unsafe"));
        AssertRetained<MoneyTransferStatusProjectionDto>(prior, failed, error, SagaId);
    }

    /// <summary>Verify that healthy balances retain the holder, value, account status and real projection version.</summary>
    [Fact]
    public void LoadedBalanceShowsObservedValueAndVersion()
    {
        using IRenderedComponent<AccountOperationsSection> cut = RenderBalance(
            CreateState(new BankAccountBalanceProjectionDto(500m, "Ada", true)));
        IElement balance = cut.Find(".spring-balance");
        Assert.Equal("Ada", balance.QuerySelector(".spring-holder")?.TextContent);
        Assert.Equal("£500.00", balance.QuerySelector("output[data-spring-balance]")?.TextContent.Trim());
        Assert.Contains("Open", balance.TextContent, StringComparison.Ordinal);
        Assert.Contains("Balance projection version 9", balance.TextContent, StringComparison.Ordinal);
        Assert.Empty(balance.QuerySelectorAll("[role='alert']"));
    }

    /// <summary>Verify that a successful empty ledger has no transaction rows and is distinct from an unavailable read.</summary>
    [Fact]
    public void LoadedEmptyLedgerShowsNoTransactions()
    {
        using IRenderedComponent<AccountOperationsSection> cut = RenderLedger(CreateState(CreateLedger(0)));
        IElement ledger = cut.Find(".spring-ledger");
        Assert.Contains("No transactions yet", ledger.TextContent, StringComparison.Ordinal);
        Assert.Empty(ledger.QuerySelectorAll("[role='alert'], [role='status'], table"));
    }

    /// <summary>Verify that successful ledger reads retain real sequence, amount, entry type and accessible column headers.</summary>
    [Fact]
    public void LoadedLedgerShowsObservedRowsAndVersion()
    {
        using IRenderedComponent<AccountOperationsSection> cut = RenderLedger(CreateState(CreateLedger(1)));
        IElement ledger = cut.Find(".spring-ledger");
        string row = Assert.Single(ledger.QuerySelectorAll("tbody tr")).TextContent;
        Assert.Contains("7", row, StringComparison.Ordinal);
        Assert.Contains("Deposit", row, StringComparison.Ordinal);
        Assert.Contains("£25.00", row, StringComparison.Ordinal);
        Assert.Contains("projection version 9", ledger.QuerySelector("caption")?.TextContent, StringComparison.Ordinal);
        Assert.Equal(3, ledger.QuerySelectorAll("th[scope='col']").Length);
        Assert.Empty(ledger.QuerySelectorAll("[role='alert'], [role='status']"));
    }

    /// <summary>Verify that loading is explicit even when the prior balance is retained in state.</summary>
    /// <param name="cached">Whether a prior successful balance is retained.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadingBalanceShowsPendingReadInsteadOfCachedLiveValue(
        bool cached
    )
    {
        BankAccountBalanceProjectionDto? projection = cached ? new(500m, "Ada", true) : null;
        ProjectionsFeatureState state = ProjectionsReducer.ReduceLoading(
            CreateState(projection),
            new ProjectionLoadingAction<BankAccountBalanceProjectionDto>(EntityId));
        using IRenderedComponent<AccountOperationsSection> cut = RenderBalance(state);
        IElement balance = cut.Find(".spring-balance");
        Assert.Contains("Loading the live account", balance.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("No account data received", balance.TextContent, StringComparison.Ordinal);
        Assert.Empty(balance.QuerySelectorAll("[role='alert'], output[data-spring-balance], .spring-holder"));
        Assert.Same(projection, state.GetProjection<BankAccountBalanceProjectionDto>(EntityId));
    }

    /// <summary>Verify that a loading ledger is distinct from missing, empty and cached transaction rows.</summary>
    /// <param name="cachedEntries">Minus one for missing data, zero for empty, or one for retained rows.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void LoadingLedgerShowsOnlyPendingRead(
        int cachedEntries
    )
    {
        BankAccountLedgerProjectionDto? projection = CreateLedger(cachedEntries);
        ProjectionsFeatureState state = ProjectionsReducer.ReduceLoading(
            CreateState(projection),
            new ProjectionLoadingAction<BankAccountLedgerProjectionDto>(EntityId));
        using IRenderedComponent<AccountOperationsSection> cut = RenderLedger(state);
        IElement ledger = cut.Find(".spring-ledger");
        Assert.Equal("Loading the ledger…", Assert.Single(ledger.QuerySelectorAll("[role='status']")).TextContent);
        Assert.DoesNotContain("No ledger data received", ledger.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("No transactions yet", ledger.TextContent, StringComparison.Ordinal);
        Assert.Empty(ledger.QuerySelectorAll("[role='alert'], table"));
        Assert.Same(projection, state.GetProjection<BankAccountLedgerProjectionDto>(EntityId));
    }

    /// <summary>Verify that missing ledger data remains unavailable rather than empty or failed.</summary>
    [Fact]
    public void MissingLedgerShowsNoData()
    {
        using IRenderedComponent<AccountOperationsSection> cut = RenderLedger(new());
        IElement ledger = cut.Find(".spring-ledger");
        Assert.Equal(
            "No ledger data received yet.",
            Assert.Single(ledger.QuerySelectorAll("[role='status']")).TextContent);
        Assert.Empty(ledger.QuerySelectorAll("[role='alert'], table"));
        Assert.DoesNotContain("No transactions yet", ledger.TextContent, StringComparison.Ordinal);
    }
}