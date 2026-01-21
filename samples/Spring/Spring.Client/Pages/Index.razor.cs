using System;
using System.Globalization;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.State;
using Spring.Client.Features.BankAccountBalance.Dtos;


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
        string.IsNullOrEmpty(AggregateState.EntityId)
            ? null
            : GetProjection<BankAccountBalanceProjectionDto>(AggregateState.EntityId);

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

            if (!string.IsNullOrEmpty(AggregateState.EntityId))
            {
                Exception? projectionError =
                    GetProjectionError<BankAccountBalanceProjectionDto>(AggregateState.EntityId);
                return projectionError?.Message;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is not established.
    /// </summary>
    private bool IsDisconnected => ConnectionState.Status != SignalRConnectionStatus.Connected;

    /// <summary>
    ///     Gets a value indicating whether any operation is in progress.
    /// </summary>
    private bool IsExecutingOrLoading =>
        AggregateState.IsExecuting ||
        (!string.IsNullOrEmpty(AggregateState.EntityId) &&
         IsProjectionLoading<BankAccountBalanceProjectionDto>(AggregateState.EntityId));

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

    private void Deposit() => Dispatch(new DepositFundsAction(AggregateState.EntityId!, depositAmount));

    private void DepositBurst20() => DispatchBurst(() => new DepositFundsAction(AggregateState.EntityId!, 5m), 20);

    private void DepositBurst200() => DispatchBurst(() => new DepositFundsAction(AggregateState.EntityId!, 10m), 200);

    private void DepositSingle100() => Dispatch(new DepositFundsAction(AggregateState.EntityId!, 100m));

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

    private string GetConnectionStatusClass() =>
        ConnectionState.Status switch
        {
            SignalRConnectionStatus.Connected => "status-badge--open",
            SignalRConnectionStatus.Connecting or SignalRConnectionStatus.Reconnecting => "status-badge--pending",
            SignalRConnectionStatus.Disconnected => "status-badge--closed",
            var _ => string.Empty,
        };

    private void ManageProjectionSubscription()
    {
        string? currentEntityId = AggregateState.EntityId;
        if (currentEntityId != subscribedEntityId)
        {
            if (!string.IsNullOrEmpty(subscribedEntityId))
            {
                UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(subscribedEntityId);
            }

            if (!string.IsNullOrEmpty(currentEntityId))
            {
                SubscribeToProjection<BankAccountBalanceProjectionDto>(currentEntityId);
            }

            subscribedEntityId = currentEntityId;
        }
    }

    private void OpenAccount() => Dispatch(new OpenAccountAction(AggregateState.EntityId!, holderName, initialDeposit));

    private void RequestReconnect() => Dispatch(new RequestSignalRConnectionAction());

    private void SetEntityId() => Dispatch(new SetEntityIdAction(accountIdInput));

    private void ToggleConnectionModal() => isConnectionModalOpen = !isConnectionModalOpen;

    private void Withdraw() => Dispatch(new WithdrawFundsAction(AggregateState.EntityId!, withdrawAmount));

    private void WithdrawBurst20() => DispatchBurst(() => new WithdrawFundsAction(AggregateState.EntityId!, 5m), 20);

    private void WithdrawBurst200() => DispatchBurst(() => new WithdrawFundsAction(AggregateState.EntityId!, 10m), 200);

    private void WithdrawSingle100() => Dispatch(new WithdrawFundsAction(AggregateState.EntityId!, 100m));
}