using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Sagas.TransferFunds.Steps;


namespace Spring.Domain.Sagas.TransferFunds.Compensations;

/// <summary>
///     Compensation for <see cref="DebitSourceAccountStep" />: refunds the source account.
/// </summary>
/// <remarks>
///     <para>
///         This compensation is invoked when the saga needs to roll back after the source
///         account was debited. It deposits the funds back into the source account.
///     </para>
///     <para>
///         If the source was never debited (<see cref="TransferFundsSagaState.SourceDebited" /> is false),
///         the compensation is skipped.
///     </para>
/// </remarks>
[SagaCompensation(typeof(DebitSourceAccountStep))]
internal sealed class RefundSourceAccountCompensation : SagaCompensationBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RefundSourceAccountCompensation" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory for resolving aggregate grains.</param>
    /// <param name="logger">The logger instance.</param>
    public RefundSourceAccountCompensation(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<RefundSourceAccountCompensation> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private ILogger<RefundSourceAccountCompensation> Logger { get; }

    /// <inheritdoc />
    public override async Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken
    )
    {
        // Only compensate if we actually debited
        if (!state.SourceDebited)
        {
            Logger.LogSkippingCompensation(context.SagaId, "Source account was not debited");
            return CompensationResult.Skipped();
        }

        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(
                state.SourceAccountId);

        OperationResult result = await grain.ExecuteAsync(
            new DepositFunds { Amount = state.Amount },
            cancellationToken);

        if (!result.Success)
        {
            Logger.LogRefundFailed(context.SagaId, state.SourceAccountId, result.ErrorMessage);
            return CompensationResult.Failed(
                result.ErrorCode ?? "REFUND_FAILED",
                result.ErrorMessage ?? "Failed to refund source account");
        }

        Logger.LogRefundSucceeded(context.SagaId, state.SourceAccountId, state.Amount);
        return CompensationResult.Succeeded();
    }
}
