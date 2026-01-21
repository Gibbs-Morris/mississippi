using System;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

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

    private decimal depositAmount;

    private string holderName = string.Empty;

    private decimal initialDeposit;

    private string? subscribedEntityId;

    private decimal withdrawAmount;

    /// <summary>
    ///     Gets the aggregate (write) feature state.
    /// </summary>
    private BankAccountAggregateState AggregateState => GetState<BankAccountAggregateState>();

    /// <summary>
    ///     Gets the SignalR connection state.
    /// </summary>
    private SignalRConnectionState ConnectionState => GetState<SignalRConnectionState>();

    /// <summary>
    ///     Gets the projection data from the InletStore.
    /// </summary>
    private BankAccountBalanceProjectionDto? BalanceProjection =>
        string.IsNullOrEmpty(AggregateState.EntityId)
            ? null
            : GetProjection<BankAccountBalanceProjectionDto>(AggregateState.EntityId);

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
    ///     Gets a value indicating whether any operation is in progress.
    /// </summary>
    private bool IsExecutingOrLoading =>
        AggregateState.IsExecuting ||
        (!string.IsNullOrEmpty(AggregateState.EntityId) &&
         IsProjectionLoading<BankAccountBalanceProjectionDto>(AggregateState.EntityId));

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
    }

    private void Deposit() => Dispatch(new DepositFundsAction(AggregateState.EntityId!, depositAmount));

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

    private void SetEntityId() => Dispatch(new SetEntityIdAction(accountIdInput));

    private void Withdraw() => Dispatch(new WithdrawFundsAction(AggregateState.EntityId!, withdrawAmount));

    private static string FormatTimestamp(
        DateTimeOffset? timestamp
    ) =>
        timestamp?.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ?? "—";

    private string GetConnectionStatusClass() =>
        ConnectionState.Status switch
        {
            SignalRConnectionStatus.Connected => "status-badge--open",
            SignalRConnectionStatus.Connecting or SignalRConnectionStatus.Reconnecting => "status-badge--pending",
            SignalRConnectionStatus.Disconnected => "status-badge--closed",
            _ => string.Empty,
        };
}