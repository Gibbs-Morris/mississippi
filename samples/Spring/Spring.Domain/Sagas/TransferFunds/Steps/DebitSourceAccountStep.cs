using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Steps;

/// <summary>
///     First step of the TransferFunds saga: debits the source account.
/// </summary>
/// <remarks>
///     <para>
///         This step executes a <see cref="WithdrawFunds" /> command against the source
///         bank account. If successful, it emits a <see cref="SourceDebited" /> event
///         to record that the debit occurred (for compensation tracking).
///     </para>
///     <para>
///         A 10-second delay is applied after step completion to enable real-time UX
///         observation of saga progression.
///     </para>
/// </remarks>
[SagaStep(1)]
[DelayAfterStep(10_000)]
internal sealed class DebitSourceAccountStep : SagaStepBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DebitSourceAccountStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory for resolving aggregate grains.</param>
    /// <param name="logger">The logger instance.</param>
    public DebitSourceAccountStep(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<DebitSourceAccountStep> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private ILogger<DebitSourceAccountStep> Logger { get; }

    /// <inheritdoc />
    public override async Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken
    )
    {
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(state.SourceAccountId);
        OperationResult result = await grain.ExecuteAsync(
            new WithdrawFunds
            {
                Amount = state.Amount,
            },
            cancellationToken);
        if (!result.Success)
        {
            Logger.LogDebitFailed(context.SagaId, state.SourceAccountId, result.ErrorMessage);
            return StepResult.Failed(
                result.ErrorCode ?? "DEBIT_FAILED",
                result.ErrorMessage ?? "Failed to debit source account");
        }

        Logger.LogDebitSucceeded(context.SagaId, state.SourceAccountId, state.Amount);
        return StepResult.Succeeded(
            new SourceDebited
            {
                Amount = state.Amount,
            });
    }
}