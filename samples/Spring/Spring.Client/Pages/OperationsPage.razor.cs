using System;
using System.Globalization;

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
using Spring.Client.Features.EntitySelection;
using Spring.Client.Features.EntitySelection.Selectors;
using Spring.Client.Features.MoneyTransferSaga.Actions;
using Spring.Client.Features.MoneyTransferStatus.Dtos;


namespace Spring.Client.Pages;

/// <summary>
///     Bank account operations page.
/// </summary>
public sealed partial class OperationsPage
{
    private decimal depositAmount;

    private string holderName = string.Empty;

    private decimal initialDeposit;

    private bool isConnectionModalOpen;

    private string? lastAutoTransferDestinationAccountId;

    private string? lastSelectedEntityId;

    private string? subscribedEntityId;

    private string? subscribedTransferSagaId;

    private decimal transferAmount;

    private string transferDestinationAccountId = string.Empty;

    private string? transferSagaId;

    private decimal withdrawAmount;

    /// <summary>
    ///     Gets the projection data from the InletStore.
    /// </summary>
    private BankAccountBalanceProjectionDto? BalanceProjection =>
        string.IsNullOrEmpty(SelectedEntityId)
            ? null
            : GetProjection<BankAccountBalanceProjectionDto>(SelectedEntityId);

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
    ///     Gets error message from aggregate state or projection error.
    /// </summary>
    private string? ErrorMessage =>
        Select<BankAccountAggregateState, ProjectionsFeatureState, string?>(
            BankAccountCompositeSelectors.GetErrorMessage(SelectedEntityId));

    /// <summary>
    ///     Gets a value indicating whether the account is open (from projection).
    /// </summary>
    private bool IsAccountOpen =>
        !string.IsNullOrEmpty(SelectedEntityId) &&
        Select(BankAccountProjectionSelectors.IsAccountOpen(SelectedEntityId));

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is not established.
    /// </summary>
    private bool IsDisconnected => Select<SignalRConnectionState, bool>(SignalRConnectionSelectors.IsDisconnected);

    /// <summary>
    ///     Gets a value indicating whether any operation is in progress.
    /// </summary>
    private bool IsExecutingOrLoading =>
        Select<BankAccountAggregateState, ProjectionsFeatureState, bool>(
            BankAccountCompositeSelectors.IsOperationInProgress(SelectedEntityId));

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
    ///     Gets the ledger projection data from the InletStore.
    /// </summary>
    private BankAccountLedgerProjectionDto? LedgerProjection =>
        string.IsNullOrEmpty(SelectedEntityId) ? null : GetProjection<BankAccountLedgerProjectionDto>(SelectedEntityId);

    /// <summary>
    ///     Gets the current reconnection attempt count.
    /// </summary>
    private int ReconnectAttemptCount =>
        Select<SignalRConnectionState, int>(SignalRConnectionSelectors.GetReconnectAttemptCount);

    /// <summary>
    ///     Gets the currently selected entity ID from entity selection state.
    /// </summary>
    private string? SelectedEntityId => Select<EntitySelectionState, string?>(EntitySelectionSelectors.GetEntityId);

    /// <summary>
    ///     Gets the transfer saga status projection for the last started transfer.
    /// </summary>
    private MoneyTransferStatusProjectionDto? TransferStatusProjection =>
        string.IsNullOrEmpty(transferSagaId) ? null : GetProjection<MoneyTransferStatusProjectionDto>(transferSagaId);

    private static string FormatTimestamp(
        DateTimeOffset? timestamp
    ) =>
        timestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing && !string.IsNullOrEmpty(subscribedEntityId))
        {
            UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(subscribedEntityId);
            UnsubscribeFromProjection<BankAccountLedgerProjectionDto>(subscribedEntityId);
        }

        if (disposing && !string.IsNullOrEmpty(subscribedTransferSagaId))
        {
            UnsubscribeFromProjection<MoneyTransferStatusProjectionDto>(subscribedTransferSagaId);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        base.OnAfterRender(firstRender);
        ManageProjectionSubscription();
        ManageTransferStatusSubscription();
        ManageDemoAccountTransferDestination();
    }

    private void ClearEntity() => Dispatch(new SetEntityIdAction(string.Empty));

    private void CloseConnectionModal() => isConnectionModalOpen = false;

    private void Deposit() => Dispatch(new DepositFundsAction(SelectedEntityId!, depositAmount));

    private void DepositBurst20() => DispatchBurst(() => new DepositFundsAction(SelectedEntityId!, 5m), 20);

    private void DepositBurst200() => DispatchBurst(() => new DepositFundsAction(SelectedEntityId!, 10m), 200);

    private void DepositSingle100() => Dispatch(new DepositFundsAction(SelectedEntityId!, 100m));

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

    private void ManageDemoAccountTransferDestination()
    {
        string? currentSelected = SelectedEntityId;
        if (string.Equals(currentSelected, lastSelectedEntityId, StringComparison.Ordinal))
        {
            return;
        }

        string? otherAccountId = Select<DemoAccountsState, string?>(
            DemoAccountsSelectors.GetOtherAccountId(currentSelected));
        if (!string.IsNullOrWhiteSpace(otherAccountId) &&
            (string.IsNullOrWhiteSpace(transferDestinationAccountId) ||
             string.Equals(
                 transferDestinationAccountId,
                 lastAutoTransferDestinationAccountId,
                 StringComparison.Ordinal)))
        {
            transferDestinationAccountId = otherAccountId;
            lastAutoTransferDestinationAccountId = otherAccountId;
        }

        lastSelectedEntityId = currentSelected;
    }

    private void ManageProjectionSubscription()
    {
        string? currentEntityId = SelectedEntityId;
        if (currentEntityId != subscribedEntityId)
        {
            if (!string.IsNullOrEmpty(subscribedEntityId))
            {
                UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(subscribedEntityId);
                UnsubscribeFromProjection<BankAccountLedgerProjectionDto>(subscribedEntityId);
            }

            if (!string.IsNullOrEmpty(currentEntityId))
            {
                SubscribeToProjection<BankAccountBalanceProjectionDto>(currentEntityId);
                SubscribeToProjection<BankAccountLedgerProjectionDto>(currentEntityId);
            }

            subscribedEntityId = currentEntityId;
        }
    }

    private void ManageTransferStatusSubscription()
    {
        string? currentSagaId = transferSagaId;
        if (currentSagaId != subscribedTransferSagaId)
        {
            if (!string.IsNullOrEmpty(subscribedTransferSagaId))
            {
                UnsubscribeFromProjection<MoneyTransferStatusProjectionDto>(subscribedTransferSagaId);
            }

            if (!string.IsNullOrEmpty(currentSagaId))
            {
                SubscribeToProjection<MoneyTransferStatusProjectionDto>(currentSagaId);
            }

            subscribedTransferSagaId = currentSagaId;
        }
    }

    private void NavigateToInvestigations() => Dispatch(new NavigateAction("/investigations"));

    private void OpenAccount() => Dispatch(new OpenAccountAction(SelectedEntityId!, holderName, initialDeposit));

    private void RequestReconnect() => Dispatch(new RequestSignalRConnectionAction());

    private void StartTransfer()
    {
        if (string.IsNullOrWhiteSpace(SelectedEntityId))
        {
            return;
        }

        Guid sagaId = Guid.NewGuid();
        transferSagaId = sagaId.ToString();
        Dispatch(
            new StartMoneyTransferSagaAction(
                sagaId,
                transferAmount,
                transferDestinationAccountId,
                SelectedEntityId,
                null));
    }

    private void ToggleConnectionModal() => isConnectionModalOpen = !isConnectionModalOpen;

    private void Withdraw() => Dispatch(new WithdrawFundsAction(SelectedEntityId!, withdrawAmount));

    private void WithdrawBurst20() => DispatchBurst(() => new WithdrawFundsAction(SelectedEntityId!, 5m), 20);

    private void WithdrawBurst200() => DispatchBurst(() => new WithdrawFundsAction(SelectedEntityId!, 10m), 200);

    private void WithdrawSingle100() => Dispatch(new WithdrawFundsAction(SelectedEntityId!, 100m));
}