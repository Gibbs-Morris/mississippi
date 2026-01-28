using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Steps;

/// <summary>
///     Step 1: Debit the source account.
/// </summary>
/// <remarks>
///     This step debits the source account for the transfer amount.
///     If the source account has insufficient funds or is not open,
///     the step fails and the saga is aborted without compensation.
/// </remarks>
[SagaStep(1, Timeout = "00:00:10")]
internal sealed class DebitSourceAccountStep : SagaStepBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DebitSourceAccountStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory to resolve aggregate grains.</param>
    public DebitSourceAccountStep(
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
        AggregateGrainFactory = aggregateGrainFactory;

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <inheritdoc />
    public override async Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken
    )
    {
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(state.SourceAccountId.ToString());
        OperationResult result = await grain.ExecuteAsync(
            new DebitForTransfer
            {
                Amount = state.Amount,
                TransferCorrelationId = context.SagaId,
                DestinationAccountId = state.DestinationAccountId,
            },
            cancellationToken);
        if (!result.Success)
        {
            return StepResult.Failed(
                result.ErrorCode ?? "DEBIT_FAILED",
                result.ErrorMessage ?? "Failed to debit source account.");
        }

        return StepResult.Succeeded(
            new SourceAccountDebited
            {
                SourceAccountId = state.SourceAccountId,
                Amount = state.Amount,
            });
    }
}