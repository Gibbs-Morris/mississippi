using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Services;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Fire-and-forget effect that sends withdrawal notifications to account holders.
/// </summary>
/// <remarks>
///     <para>
///         This effect demonstrates the fire-and-forget pattern where notifications are sent
///         asynchronously without blocking the command response. The withdrawal completes
///         immediately, and the customer receives a notification shortly after.
///     </para>
///     <para>
///         Unlike <see cref="HighValueTransactionEffect" /> which uses synchronous effects,
///         this effect runs in a separate worker grain. If the notification fails (network issues,
///         service unavailable), it does not affect the withdrawal operation itself.
///     </para>
///     <para>
///         This effect uses <see cref="FireAndForgetEventEffectBase{TEvent,TAggregate}" /> because:
///         <list type="bullet">
///             <item>Notification delivery shouldn't block command response latency</item>
///             <item>Notification failure shouldn't fail the withdrawal</item>
///             <item>Notifications are "nice to have" not critical business logic</item>
///         </list>
///     </para>
/// </remarks>
internal sealed class WithdrawalNotificationEffect : FireAndForgetEventEffectBase<FundsWithdrawn, BankAccountAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WithdrawalNotificationEffect" /> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending alerts.</param>
    /// <param name="logger">The logger instance.</param>
    public WithdrawalNotificationEffect(
        INotificationService notificationService,
        ILogger<WithdrawalNotificationEffect> logger
    )
    {
        NotificationService = notificationService;
        Logger = logger;
    }

    private ILogger<WithdrawalNotificationEffect> Logger { get; }

    private INotificationService NotificationService { get; }

    /// <inheritdoc />
    public override async Task HandleAsync(
        FundsWithdrawn eventData,
        BankAccountAggregate aggregateState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(aggregateState);

        // Extract the entity ID (account ID) from the brook key
        string accountId = BrookKey.FromString(brookKey).EntityId;
        Logger.LogSendingWithdrawalNotification(accountId, eventData.Amount, aggregateState.Balance);
        try
        {
            await NotificationService.SendWithdrawalAlertAsync(
                accountId,
                eventData.Amount,
                aggregateState.Balance,
                cancellationToken);
            Logger.LogWithdrawalNotificationSent(accountId, eventData.Amount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log but don't rethrow - notification failures shouldn't break the system
            Logger.LogWithdrawalNotificationFailed(accountId, eventData.Amount, ex.Message);
        }
    }
}