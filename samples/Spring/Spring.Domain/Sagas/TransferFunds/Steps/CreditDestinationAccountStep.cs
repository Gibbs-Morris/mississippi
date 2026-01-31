using System;
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
///     Second step of the TransferFunds saga: credits the destination account.
/// </summary>
/// <remarks>
///     <para>
///         This step executes a <see cref="DepositFunds" /> command against the destination
///         bank account. If successful, it emits <see cref="DestinationCredited" /> and
///         <see cref="TransferCompleted" /> events to complete the saga.
///     </para>
/// </remarks>
[SagaStep(2)]
internal sealed class CreditDestinationAccountStep : SagaStepBase<TransferFundsSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CreditDestinationAccountStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">The factory for resolving aggregate grains.</param>
    /// <param name="logger">The logger instance.</param>
    public CreditDestinationAccountStep(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<CreditDestinationAccountStep> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory;
        Logger = logger;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private ILogger<CreditDestinationAccountStep> Logger { get; }

    /// <inheritdoc />
    public override async Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TransferFundsSagaState state,
        CancellationToken cancellationToken
    )
    {
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(
                state.DestinationAccountId);

        OperationResult result = await grain.ExecuteAsync(
            new DepositFunds { Amount = state.Amount },
            cancellationToken);

        if (!result.Success)
        {
            Logger.LogCreditFailed(context.SagaId, state.DestinationAccountId, result.ErrorMessage);
            return StepResult.Failed(
                result.ErrorCode ?? "CREDIT_FAILED",
                result.ErrorMessage ?? "Failed to credit destination account");
        }

        Logger.LogCreditSucceeded(context.SagaId, state.DestinationAccountId, state.Amount);
        return StepResult.Succeeded(
            new DestinationCredited { Amount = state.Amount },
            new TransferCompleted { CompletedAt = DateTimeOffset.UtcNow });
    }
}
