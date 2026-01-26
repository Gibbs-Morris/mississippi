using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans.Runtime;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.TransactionInvestigationQueue;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     Effect that flags high-value deposits for manual investigation.
/// </summary>
/// <remarks>
///     <para>
///         This effect demonstrates cross-aggregate command dispatch from an event effect.
///         When a deposit exceeds the AML threshold (£10,000), the effect dispatches a
///         <see cref="FlagTransaction" /> command to the singleton
///         <see cref="TransactionInvestigationQueueAggregate" />.
///     </para>
///     <para>
///         The effect runs asynchronously after the original <see cref="FundsDeposited" /> event
///         is persisted, ensuring the deposit completes immediately while flagging happens
///         in the background.
///     </para>
///     <para>
///         This effect uses <see cref="SimpleEventEffectBase{TEvent,TAggregate}" /> because it
///         performs side operations (command dispatch, logging) without yielding additional events.
///     </para>
/// </remarks>
internal sealed class HighValueTransactionEffect : SimpleEventEffectBase<FundsDeposited, BankAccountAggregate>
{
    /// <summary>
    ///     The AML threshold amount in GBP. Deposits exceeding this amount are flagged.
    /// </summary>
    internal const decimal AmlThreshold = 10_000m;

    /// <summary>
    ///     The singleton entity ID for the investigation queue aggregate.
    /// </summary>
    private const string InvestigationQueueEntityId = "global";

    /// <summary>
    ///     Initializes a new instance of the <see cref="HighValueTransactionEffect" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="grainContext">The Orleans grain context providing access to the current grain identity.</param>
    /// <param name="logger">The logger instance.</param>
    public HighValueTransactionEffect(
        IAggregateGrainFactory aggregateGrainFactory,
        IGrainContext grainContext,
        ILogger<HighValueTransactionEffect> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory;
        GrainContext = grainContext;
        Logger = logger;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private IGrainContext GrainContext { get; }

    private ILogger<HighValueTransactionEffect> Logger { get; }

    /// <inheritdoc />
    protected override async Task HandleSimpleAsync(
        FundsDeposited eventData,
        BankAccountAggregate currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Amount <= AmlThreshold)
        {
            return;
        }

        // Extract the account ID from the grain's primary key
        string accountId = GrainContext.GrainId.Key.ToString() ?? string.Empty;
        Logger.LogHighValueTransactionDetected(accountId, eventData.Amount, AmlThreshold);
        FlagTransaction command = new()
        {
            AccountId = accountId,
            Amount = eventData.Amount,
            Timestamp = DateTimeOffset.UtcNow,
        };
        IGenericAggregateGrain<TransactionInvestigationQueueAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<TransactionInvestigationQueueAggregate>(
                InvestigationQueueEntityId);
        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            Logger.LogFlagTransactionFailed(
                accountId,
                eventData.Amount,
                result.ErrorCode,
                result.ErrorMessage ?? "Unknown error");
        }
        else
        {
            Logger.LogTransactionFlagged(accountId, eventData.Amount);
        }
    }
}