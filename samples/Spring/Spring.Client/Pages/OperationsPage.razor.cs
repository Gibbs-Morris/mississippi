using System;
using System.Globalization;

using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Selectors;
using Spring.Client.Features.BankAccountAggregate.State;
using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.BankAccountBalance.Selectors;
using Spring.Client.Features.BankAccountLedger.Dtos;
using Spring.Client.Features.DemoAccounts;
using Spring.Client.Features.DualEntitySelection;
using Spring.Client.Features.DualEntitySelection.Selectors;
using Spring.Client.Features.MoneyTransferSaga.Actions;
using Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace Spring.Client.Pages;

/// <summary>
///     Bank account operations page.
/// </summary>
public sealed partial class OperationsPage
{
    private readonly AccountPanelState panelA = new();

    private readonly AccountPanelState panelB = new();

    private bool isConnectionModalOpen;

    private string? lastAutoAccountAId;

    private string? lastAutoAccountBId;

    private string? lastNavigatedAccountAId;

    private string? lastNavigatedAccountBId;

    private string? lastQueryAccountAId;

    private string? lastQueryAccountBId;

    private string? subscribedEntityIdA;

    private string? subscribedEntityIdB;

    private string? subscribedTransferSagaIdA;

    private string? subscribedTransferSagaIdB;

    /// <summary>
    ///     Gets or sets the account A identifier from the query string.
    /// </summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "a")]
    public string? AccountAIdQuery { get; set; }

    /// <summary>
    ///     Gets or sets the account B identifier from the query string.
    /// </summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "b")]
    public string? AccountBIdQuery { get; set; }

    /// <summary>
    ///     Gets account A identifier from selection state.
    /// </summary>
    private string? AccountAId => Select<DualEntitySelectionState, string?>(DualEntitySelectionSelectors.GetAccountAId);

    /// <summary>
    ///     Gets account A identifier as a non-null display string.
    /// </summary>
    private string AccountAIdDisplay => AccountAId ?? string.Empty;

    /// <summary>
    ///     Gets account B identifier from selection state.
    /// </summary>
    private string? AccountBId => Select<DualEntitySelectionState, string?>(DualEntitySelectionSelectors.GetAccountBId);

    /// <summary>
    ///     Gets account B identifier as a non-null display string.
    /// </summary>
    private string AccountBIdDisplay => AccountBId ?? string.Empty;

    /// <summary>
    ///     Gets the balance projection for account A.
    /// </summary>
    private BankAccountBalanceProjectionDto? BalanceProjectionA => GetBalanceProjection(AccountAId);

    /// <summary>
    ///     Gets the balance projection for account B.
    /// </summary>
    private BankAccountBalanceProjectionDto? BalanceProjectionB => GetBalanceProjection(AccountBId);

    /// <summary>
    ///     Gets the current connection identifier.
    /// </summary>
    private string? ConnectionId => Select<SignalRConnectionState, string?>(SignalRConnectionSelectors.GetConnectionId);

    /// <summary>
    ///     Gets the connection identifier display value.
    /// </summary>
    private string ConnectionIdDisplay => ConnectionId ?? "—";

    /// <summary>
    ///     Gets the current connection status.
    /// </summary>
    private SignalRConnectionStatus ConnectionStatus =>
        Select<SignalRConnectionState, SignalRConnectionStatus>(SignalRConnectionSelectors.GetStatus);

    /// <summary>
    ///     Gets a value indicating whether both accounts are selected.
    /// </summary>
    private bool HasAccountPair => Select<DualEntitySelectionState, bool>(DualEntitySelectionSelectors.HasAccountPair);

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is not established.
    /// </summary>
    private bool IsDisconnected => Select<SignalRConnectionState, bool>(SignalRConnectionSelectors.IsDisconnected);

    /// <summary>
    ///     Gets a value indicating whether the last command succeeded.
    /// </summary>
    private bool? LastCommandSucceeded =>
        Select<BankAccountAggregateState, bool?>(BankAccountAggregateSelectors.DidLastCommandSucceed);

    /// <summary>
    ///     Gets the timestamp when the connection was last successfully established.
    /// </summary>
    private DateTimeOffset? LastConnectedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastConnectedAt);

    /// <summary>
    ///     Gets the last connected timestamp display value.
    /// </summary>
    private string LastConnectedAtDisplay => FormatTimestamp(LastConnectedAt);

    /// <summary>
    ///     Gets the timestamp when the connection was last disconnected.
    /// </summary>
    private DateTimeOffset? LastDisconnectedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastDisconnectedAt);

    /// <summary>
    ///     Gets the last disconnected timestamp display value.
    /// </summary>
    private string LastDisconnectedAtDisplay => FormatTimestamp(LastDisconnectedAt);

    /// <summary>
    ///     Gets the last error message from the connection.
    /// </summary>
    private string? LastError => Select<SignalRConnectionState, string?>(SignalRConnectionSelectors.GetLastError);

    /// <summary>
    ///     Gets the timestamp when the last message was received.
    /// </summary>
    private DateTimeOffset? LastMessageReceivedAt =>
        Select<SignalRConnectionState, DateTimeOffset?>(SignalRConnectionSelectors.GetLastMessageReceivedAt);

    /// <summary>
    ///     Gets the last message timestamp display value.
    /// </summary>
    private string LastMessageReceivedAtDisplay => FormatTimestamp(LastMessageReceivedAt);

    /// <summary>
    ///     Gets the ledger projection for account A.
    /// </summary>
    private BankAccountLedgerProjectionDto? LedgerProjectionA => GetLedgerProjection(AccountAId);

    /// <summary>
    ///     Gets the ledger projection for account B.
    /// </summary>
    private BankAccountLedgerProjectionDto? LedgerProjectionB => GetLedgerProjection(AccountBId);

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    ///     Gets the current reconnection attempt count.
    /// </summary>
    private int ReconnectAttemptCount =>
        Select<SignalRConnectionState, int>(SignalRConnectionSelectors.GetReconnectAttemptCount);

    /// <summary>
    ///     Gets the transfer status projection for account A.
    /// </summary>
    private MoneyTransferStatusProjectionDto? TransferStatusProjectionA =>
        GetTransferStatusProjection(panelA.TransferSagaId);

    /// <summary>
    ///     Gets the transfer status projection for account B.
    /// </summary>
    private MoneyTransferStatusProjectionDto? TransferStatusProjectionB =>
        GetTransferStatusProjection(panelB.TransferSagaId);

    private static string BuildOperationsUrl(
        string? accountAId,
        string? accountBId
    )
    {
        string? encodedA = NormalizeAccountId(accountAId) is { } valueA ? Uri.EscapeDataString(valueA) : null;
        string? encodedB = NormalizeAccountId(accountBId) is { } valueB ? Uri.EscapeDataString(valueB) : null;
        string url = "/operations";
        if (encodedA is null && encodedB is null)
        {
            return url;
        }

        if (encodedA is not null)
        {
            url += $"?a={encodedA}";
        }

        if (encodedB is not null)
        {
            string separator = encodedA is null ? "?" : "&";
            url += $"{separator}b={encodedB}";
        }

        return url;
    }

    private static string FormatTimestamp(
        DateTimeOffset? timestamp
    ) =>
        timestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";

    private static string? NormalizeAccountId(
        string? accountId
    ) =>
        string.IsNullOrWhiteSpace(accountId) ? null : accountId.Trim();

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing)
        {
            UnsubscribeFromAccountProjections(subscribedEntityIdA);
            UnsubscribeFromAccountProjections(subscribedEntityIdB);
            UnsubscribeFromTransferSaga(subscribedTransferSagaIdA);
            UnsubscribeFromTransferSaga(subscribedTransferSagaIdB);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        base.OnAfterRender(firstRender);
        ManageProjectionSubscriptions();
        ManageTransferStatusSubscriptions();
        ManageDemoAccountSelection();
        SyncTransferDestinations();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SyncAccountIdsFromQuery();
    }

    private void ClearAccountA() => Dispatch(new SetEntityAIdAction(string.Empty));

    private void ClearAccountB() => Dispatch(new SetEntityBIdAction(string.Empty));

    private void CloseConnectionModal() => isConnectionModalOpen = false;

    private void DepositA() => DepositForPanel(AccountAId, panelA.DepositAmount);

    private void DepositB() => DepositForPanel(AccountBId, panelB.DepositAmount);

    private void DepositBurst200A() =>
        DispatchBurstFor(AccountAId, () => new DepositFundsAction(AccountAId!, 10m), 200);

    private void DepositBurst200B() =>
        DispatchBurstFor(AccountBId, () => new DepositFundsAction(AccountBId!, 10m), 200);

    private void DepositBurst20A() => DispatchBurstFor(AccountAId, () => new DepositFundsAction(AccountAId!, 5m), 20);

    private void DepositBurst20B() => DispatchBurstFor(AccountBId, () => new DepositFundsAction(AccountBId!, 5m), 20);

    private void DepositForPanel(
        string? accountId,
        decimal amount
    )
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return;
        }

        Dispatch(new DepositFundsAction(accountId, amount));
    }

    private void DepositSingle100A() => DepositForPanel(AccountAId, 100m);

    private void DepositSingle100B() => DepositForPanel(AccountBId, 100m);

    /// <summary>
    ///     Dispatches multiple actions in rapid succession for stress-testing the event sourcing pipeline.
    ///     This intentionally has no delay between dispatches to demonstrate SignalR projection update
    ///     throughput and Orleans grain handling under load.
    /// </summary>
    /// <param name="actionFactory">Factory function creating the action to dispatch.</param>
    /// <param name="count">Number of times to dispatch the action.</param>
    private void DispatchBurst(
        Func<IAction> actionFactory,
        int count
    )
    {
        for (int i = 0; i < count; i++)
        {
            Dispatch(actionFactory());
        }
    }

    private void DispatchBurstFor(
        string? accountId,
        Func<IAction> actionFactory,
        int count
    )
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return;
        }

        DispatchBurst(actionFactory, count);
    }

    private BankAccountBalanceProjectionDto? GetBalanceProjection(
        string? accountId
    ) =>
        string.IsNullOrEmpty(accountId) ? null : GetProjection<BankAccountBalanceProjectionDto>(accountId);

    private string? GetErrorMessageFor(
        string? accountId
    ) =>
        Select<BankAccountAggregateState, ProjectionsFeatureState, string?>(
            BankAccountCompositeSelectors.GetErrorMessage(accountId));

    private BankAccountLedgerProjectionDto? GetLedgerProjection(
        string? accountId
    ) =>
        string.IsNullOrEmpty(accountId) ? null : GetProjection<BankAccountLedgerProjectionDto>(accountId);

    private MoneyTransferStatusProjectionDto? GetTransferStatusProjection(
        string? sagaId
    ) =>
        string.IsNullOrEmpty(sagaId) ? null : GetProjection<MoneyTransferStatusProjectionDto>(sagaId);

    private bool IsAccountOpenFor(
        string? accountId
    ) =>
        !string.IsNullOrWhiteSpace(accountId) && Select(BankAccountProjectionSelectors.IsAccountOpen(accountId));

    private bool IsExecutingOrLoadingFor(
        string? accountId
    ) =>
        Select<BankAccountAggregateState, ProjectionsFeatureState, bool>(
            BankAccountCompositeSelectors.IsOperationInProgress(accountId));

    private void ManageDemoAccountSelection()
    {
        string? demoAccountAId = Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountAId);
        string? demoAccountBId = Select<DemoAccountsState, string?>(DemoAccountsSelectors.GetAccountBId);
        if ((string.IsNullOrWhiteSpace(AccountAId) || string.IsNullOrWhiteSpace(AccountBId)) &&
            (!string.Equals(demoAccountAId, lastAutoAccountAId, StringComparison.Ordinal) ||
             !string.Equals(demoAccountBId, lastAutoAccountBId, StringComparison.Ordinal)))
        {
            lastAutoAccountAId = demoAccountAId;
            lastAutoAccountBId = demoAccountBId;
            if (!string.IsNullOrWhiteSpace(demoAccountAId) || !string.IsNullOrWhiteSpace(demoAccountBId))
            {
                SetAccountPair(demoAccountAId, demoAccountBId, true);
            }
        }
    }

    private void ManageProjectionSubscriptions()
    {
        SyncProjectionSubscription(AccountAId, ref subscribedEntityIdA);
        SyncProjectionSubscription(AccountBId, ref subscribedEntityIdB);
    }

    private void ManageTransferStatusSubscriptions()
    {
        SyncTransferSubscription(panelA.TransferSagaId, ref subscribedTransferSagaIdA);
        SyncTransferSubscription(panelB.TransferSagaId, ref subscribedTransferSagaIdB);
    }

    private void NavigateToInvestigations() => Dispatch(new NavigateAction("/investigations"));

    private void OpenAccountA() => OpenAccountFor(panelA, AccountAId);

    private void OpenAccountB() => OpenAccountFor(panelB, AccountBId);

    private void OpenAccountFor(
        AccountPanelState panel,
        string? accountId
    )
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return;
        }

        Dispatch(new OpenAccountAction(accountId, panel.HolderName, panel.InitialDeposit));
    }

    private void RequestReconnect() => Dispatch(new RequestSignalRConnectionAction());

    private void SetAccountPair(
        string? accountAId,
        string? accountBId,
        bool updateUrl
    )
    {
        string? normalizedA = NormalizeAccountId(accountAId);
        string? normalizedB = NormalizeAccountId(accountBId);
        if (!string.Equals(AccountAId, normalizedA, StringComparison.Ordinal))
        {
            Dispatch(new SetEntityAIdAction(normalizedA ?? string.Empty));
        }

        if (!string.Equals(AccountBId, normalizedB, StringComparison.Ordinal))
        {
            Dispatch(new SetEntityBIdAction(normalizedB ?? string.Empty));
        }

        if (updateUrl)
        {
            UpdateShareUrl(normalizedA, normalizedB);
        }
    }

    private void StartTransferA() => StartTransferForPanel(panelA, AccountAId, AccountBId);

    private void StartTransferB() => StartTransferForPanel(panelB, AccountBId, AccountAId);

    private void StartTransferForPanel(
        AccountPanelState panel,
        string? sourceAccountId,
        string? destinationAccountId
    )
    {
        if (string.IsNullOrWhiteSpace(sourceAccountId) || string.IsNullOrWhiteSpace(destinationAccountId))
        {
            return;
        }

        Guid sagaId = Guid.NewGuid();
        panel.TransferSagaId = sagaId.ToString();
        Dispatch(
            new StartMoneyTransferSagaAction(
                sagaId,
                panel.TransferAmount,
                destinationAccountId,
                sourceAccountId,
                null));
    }

    private void SyncAccountIdsFromQuery()
    {
        string? queryA = NormalizeAccountId(AccountAIdQuery);
        string? queryB = NormalizeAccountId(AccountBIdQuery);
        if (!string.Equals(queryA, lastQueryAccountAId, StringComparison.Ordinal) ||
            !string.Equals(queryB, lastQueryAccountBId, StringComparison.Ordinal))
        {
            lastQueryAccountAId = queryA;
            lastQueryAccountBId = queryB;
            if (queryA is not null || queryB is not null)
            {
                SetAccountPair(queryA, queryB, false);
            }
        }
    }

    private void SyncProjectionSubscription(
        string? currentEntityId,
        ref string? subscribedEntityId
    )
    {
        if (string.Equals(currentEntityId, subscribedEntityId, StringComparison.Ordinal))
        {
            return;
        }

        UnsubscribeFromAccountProjections(subscribedEntityId);
        if (!string.IsNullOrWhiteSpace(currentEntityId))
        {
            SubscribeToProjection<BankAccountBalanceProjectionDto>(currentEntityId);
            SubscribeToProjection<BankAccountLedgerProjectionDto>(currentEntityId);
        }

        subscribedEntityId = currentEntityId;
    }

    private void SyncTransferDestinations()
    {
        panelA.TransferDestinationAccountId = AccountBId ?? string.Empty;
        panelB.TransferDestinationAccountId = AccountAId ?? string.Empty;
    }

    private void SyncTransferSubscription(
        string? currentSagaId,
        ref string? subscribedSagaId
    )
    {
        if (string.Equals(currentSagaId, subscribedSagaId, StringComparison.Ordinal))
        {
            return;
        }

        UnsubscribeFromTransferSaga(subscribedSagaId);
        if (!string.IsNullOrWhiteSpace(currentSagaId))
        {
            SubscribeToProjection<MoneyTransferStatusProjectionDto>(currentSagaId);
        }

        subscribedSagaId = currentSagaId;
    }

    private void ToggleConnectionModal() => isConnectionModalOpen = !isConnectionModalOpen;

    private void UnsubscribeFromAccountProjections(
        string? entityId
    )
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return;
        }

        UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(entityId);
        UnsubscribeFromProjection<BankAccountLedgerProjectionDto>(entityId);
    }

    private void UnsubscribeFromTransferSaga(
        string? sagaId
    )
    {
        if (!string.IsNullOrWhiteSpace(sagaId))
        {
            UnsubscribeFromProjection<MoneyTransferStatusProjectionDto>(sagaId);
        }
    }

    private void UpdateShareUrl(
        string? accountAId,
        string? accountBId
    )
    {
        if (string.Equals(accountAId, lastNavigatedAccountAId, StringComparison.Ordinal) &&
            string.Equals(accountBId, lastNavigatedAccountBId, StringComparison.Ordinal))
        {
            return;
        }

        lastNavigatedAccountAId = accountAId;
        lastNavigatedAccountBId = accountBId;
        string url = BuildOperationsUrl(accountAId, accountBId);
        NavigationManager.NavigateTo(url, replace: true);
    }

    private void WithdrawA() => WithdrawForPanel(AccountAId, panelA.WithdrawAmount);

    private void WithdrawB() => WithdrawForPanel(AccountBId, panelB.WithdrawAmount);

    private void WithdrawBurst200A() =>
        DispatchBurstFor(AccountAId, () => new WithdrawFundsAction(AccountAId!, 10m), 200);

    private void WithdrawBurst200B() =>
        DispatchBurstFor(AccountBId, () => new WithdrawFundsAction(AccountBId!, 10m), 200);

    private void WithdrawBurst20A() => DispatchBurstFor(AccountAId, () => new WithdrawFundsAction(AccountAId!, 5m), 20);

    private void WithdrawBurst20B() => DispatchBurstFor(AccountBId, () => new WithdrawFundsAction(AccountBId!, 5m), 20);

    private void WithdrawForPanel(
        string? accountId,
        decimal amount
    )
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return;
        }

        Dispatch(new WithdrawFundsAction(accountId, amount));
    }

    private void WithdrawSingle100A() => WithdrawForPanel(AccountAId, 100m);

    private void WithdrawSingle100B() => WithdrawForPanel(AccountBId, 100m);

    private sealed class AccountPanelState
    {
        public decimal DepositAmount { get; set; }

        public string HolderName { get; set; } = string.Empty;

        public decimal InitialDeposit { get; set; }

        public decimal TransferAmount { get; set; }

        public string TransferDestinationAccountId { get; set; } = string.Empty;

        public string? TransferSagaId { get; set; }

        public decimal WithdrawAmount { get; set; }
    }
}