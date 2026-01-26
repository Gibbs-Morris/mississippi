using System;
using System.Globalization;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.State;
using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.BankAccountLedger.Dtos;
using Spring.Client.Features.EntitySelection;


namespace Spring.Client.Pages;

/// <summary>
///     Bank Account Demo page showcasing real-time event-sourced banking operations.
/// </summary>
/// <remarks>
///     <para>
///         This page demonstrates the Mississippi stack patterns:
///     </para>
///     <list type="bullet">
///         <item>Redux-style state management via Reservoir</item>
///         <item>Real-time projection synchronization via Inlet/SignalR</item>
///         <item>Command dispatch with optimistic UI feedback</item>
///     </list>
/// </remarks>
public sealed partial class Index
{
    private string accountIdInput = string.Empty;

    private bool balanceJustChanged;

    private decimal depositAmount;

    private bool holderJustChanged;

    private string holderName = string.Empty;

    private decimal initialDeposit;

    private bool isConnectionModalOpen;

    private decimal? previousBalance;

    private string? previousHolderName;

    private bool? previousIsOpen;

    private bool statusJustChanged;

    private string? subscribedEntityId;

    private decimal withdrawAmount;

    /// <summary>
    ///     Gets the aggregate (write) feature state.
    /// </summary>
    private BankAccountAggregateState AggregateState => GetState<BankAccountAggregateState>();

    /// <summary>
    ///     Gets the projection data from the InletStore.
    /// </summary>
    private BankAccountBalanceProjectionDto? BalanceProjection =>
        string.IsNullOrEmpty(SelectedEntityId)
            ? null
            : GetProjection<BankAccountBalanceProjectionDto>(SelectedEntityId);

    /// <summary>
    ///     Gets the SignalR connection state.
    /// </summary>
    private SignalRConnectionState ConnectionState => GetState<SignalRConnectionState>();

    /// <summary>
    ///     Gets error message from aggregate state or projection error.
    /// </summary>
    private string? ErrorMessage
    {
        get
        {
            if (!string.IsNullOrEmpty(AggregateState.ErrorMessage))
            {
                return AggregateState.ErrorMessage;
            }

            if (!string.IsNullOrEmpty(SelectedEntityId))
            {
                Exception? projectionError = GetProjectionError<BankAccountBalanceProjectionDto>(SelectedEntityId);
                return projectionError?.Message;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the account is open (from projection).
    /// </summary>
    private bool IsAccountOpen => BalanceProjection?.IsOpen is true;

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is not established.
    /// </summary>
    private bool IsDisconnected => ConnectionState.Status != SignalRConnectionStatus.Connected;

    /// <summary>
    ///     Gets a value indicating whether any operation is in progress.
    /// </summary>
    private bool IsExecutingOrLoading =>
        AggregateState.IsExecuting ||
        (!string.IsNullOrEmpty(SelectedEntityId) &&
         IsProjectionLoading<BankAccountBalanceProjectionDto>(SelectedEntityId));

    /// <summary>
    ///     Gets the ledger projection data from the InletStore.
    /// </summary>
    private BankAccountLedgerProjectionDto? LedgerProjection =>
        string.IsNullOrEmpty(SelectedEntityId) ? null : GetProjection<BankAccountLedgerProjectionDto>(SelectedEntityId);

    /// <summary>
    ///     Gets the currently selected entity ID from entity selection state.
    /// </summary>
    private string? SelectedEntityId => GetState<EntitySelectionState>().EntityId;

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

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        base.OnAfterRender(firstRender);
        ManageProjectionSubscription();
        DetectProjectionChanges();
    }

    private void ClearEntity() => Dispatch(new SetEntityIdAction(string.Empty));

    private void CloseConnectionModal() => isConnectionModalOpen = false;

    private void Deposit() => Dispatch(new DepositCashAction(SelectedEntityId!, depositAmount));

    private void DepositBurst20() => DispatchBurst(() => new DepositCashAction(SelectedEntityId!, 5m), 20);

    private void DepositBurst200() => DispatchBurst(() => new DepositCashAction(SelectedEntityId!, 10m), 200);

    private void DepositSingle100() => Dispatch(new DepositCashAction(SelectedEntityId!, 100m));

    /// <summary>
    ///     Detects when projection values change and triggers update animations.
    /// </summary>
    private void DetectProjectionChanges()
    {
        decimal? currentBalance = BalanceProjection?.Balance;
        string? currentHolder = BalanceProjection?.HolderName;
        bool? currentIsOpen = BalanceProjection?.IsOpen;
        bool needsRerender = false;

        // Detect balance change
        if (previousBalance.HasValue && currentBalance.HasValue && (previousBalance.Value != currentBalance.Value))
        {
            balanceJustChanged = true;
            needsRerender = true;
        }
        else if (balanceJustChanged)
        {
            balanceJustChanged = false;
        }

        // Detect holder name change
        if (previousHolderName is not null &&
            currentHolder is not null &&
            !string.Equals(previousHolderName, currentHolder, StringComparison.Ordinal))
        {
            holderJustChanged = true;
            needsRerender = true;
        }
        else if (holderJustChanged)
        {
            holderJustChanged = false;
        }

        // Detect status (isOpen) change
        if (previousIsOpen.HasValue && currentIsOpen.HasValue && (previousIsOpen.Value != currentIsOpen.Value))
        {
            statusJustChanged = true;
            needsRerender = true;
        }
        else if (statusJustChanged)
        {
            statusJustChanged = false;
        }

        previousBalance = currentBalance;
        previousHolderName = currentHolder;
        previousIsOpen = currentIsOpen;
        if (needsRerender)
        {
            StateHasChanged();
        }
    }

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

    private void OpenAccount() => Dispatch(new OpenAccountAction(SelectedEntityId!, holderName, initialDeposit));

    private void RequestReconnect() => Dispatch(new RequestSignalRConnectionAction());

    private void SetEntityId() => Dispatch(new SetEntityIdAction(accountIdInput));

    private void ToggleConnectionModal() => isConnectionModalOpen = !isConnectionModalOpen;

    private void Withdraw() => Dispatch(new WithdrawCashAction(SelectedEntityId!, withdrawAmount));

    private void WithdrawBurst20() => DispatchBurst(() => new WithdrawCashAction(SelectedEntityId!, 5m), 20);

    private void WithdrawBurst200() => DispatchBurst(() => new WithdrawCashAction(SelectedEntityId!, 10m), 200);

    private void WithdrawSingle100() => Dispatch(new WithdrawCashAction(SelectedEntityId!, 100m));
}