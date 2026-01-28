using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Sagas.TransferFunds.Steps;


namespace Spring.Domain.Sagas.TransferFunds.Compensations;

/// <summary>
///     Compensation for <see cref="Steps.DebitSourceAccountStep" />: refund the source account.
/// </summary>
/// <remarks>
///     This compensation is invoked when the transfer saga fails after the source account
///     was debited but before or during the credit to the destination account.
///     It restores the funds to the source account.
/// </remarks>
[SagaCompensation(typeof(DebitSourceAccountStep))]
internal sealed class RefundSourceAccountCompensation : SagaCompensationBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RefundSourceAccountCompensation" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory to resolve aggregate grains.</param>
    public RefundSourceAccountCompensation(
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
        AggregateGrainFactory = aggregateGrainFactory;

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <inheritdoc />
    public override async Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken
    )
    {
        // Skip if source was never debited
        if (!state.SourceDebited)
        {
            return CompensationResult.Skipped();
        }

        // Skip if already refunded (idempotency)
        if (state.SourceRefunded)
        {
            return CompensationResult.Skipped();
        }

        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(state.SourceAccountId.ToString());
        OperationResult result = await grain.ExecuteAsync(
            new RefundTransfer
            {
                Amount = state.Amount,
                TransferCorrelationId = context.SagaId,
                Reason = state.FailureReason ?? "Transfer rolled back.",
            },
            cancellationToken);
        if (!result.Success)
        {
            return CompensationResult.Failed(
                result.ErrorCode ?? "REFUND_FAILED",
                result.ErrorMessage ?? "Failed to refund source account.");
        }

        return CompensationResult.Succeeded();
    }
}