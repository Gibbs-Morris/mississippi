using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Steps;

/// <summary>
///     Step 2: Credit the destination account.
/// </summary>
/// <remarks>
///     This step credits the destination account with the transfer amount.
///     If the destination account is not open, the step fails and
///     compensation is triggered to refund the source account.
/// </remarks>
[SagaStep(2, Timeout = "00:00:10")]
internal sealed class CreditDestinationAccountStep : SagaStepBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CreditDestinationAccountStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory to resolve aggregate grains.</param>
    public CreditDestinationAccountStep(
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
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(state.DestinationAccountId.ToString());
        OperationResult result = await grain.ExecuteAsync(
            new CreditFromTransfer
            {
                Amount = state.Amount,
                TransferCorrelationId = context.SagaId,
                SourceAccountId = state.SourceAccountId,
            },
            cancellationToken);
        if (!result.Success)
        {
            return StepResult.Failed(
                result.ErrorCode ?? "CREDIT_FAILED",
                result.ErrorMessage ?? "Failed to credit destination account.");
        }

        return StepResult.Succeeded(
            new DestinationAccountCredited
            {
                DestinationAccountId = state.DestinationAccountId,
                Amount = state.Amount,
            });
    }
}