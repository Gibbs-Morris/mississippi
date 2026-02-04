using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.MoneyTransferSaga.Commands;


namespace Spring.Domain.Aggregates.MoneyTransferSaga.Steps;

/// <summary>
///     Saga step that withdraws funds from the source account.
/// </summary>
[SagaStep<MoneyTransferSagaState>(0)]
internal sealed class WithdrawFromSourceStep
    : ISagaStep<MoneyTransferSagaState>,
      ICompensatable<MoneyTransferSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WithdrawFromSourceStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving bank account aggregates.</param>
    public WithdrawFromSourceStep(
        IAggregateGrainFactory aggregateGrainFactory
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        AggregateGrainFactory = aggregateGrainFactory;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <inheritdoc />
    public async Task<CompensationResult> CompensateAsync(
        MoneyTransferSagaState state,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
        {
            return CompensationResult.SkippedResult("Transfer input not provided.");
        }

        DepositFunds command = new()
        {
            Amount = input.Amount,
        };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(input.SourceAccountId);
        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            return CompensationResult.Failed(result.ErrorCode ?? "COMPENSATION_FAILED", result.ErrorMessage);
        }

        return CompensationResult.Succeeded();
    }

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        MoneyTransferSagaState state,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidState, "Transfer input not provided.");
        }

        if (string.IsNullOrWhiteSpace(input.SourceAccountId) || string.IsNullOrWhiteSpace(input.DestinationAccountId))
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand, "Account identifiers are required.");
        }

        if (string.Equals(input.SourceAccountId, input.DestinationAccountId, StringComparison.Ordinal))
        {
            return StepResult.Failed(
                AggregateErrorCodes.InvalidCommand,
                "Source and destination accounts must differ.");
        }

        if (input.Amount <= 0)
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand, "Transfer amount must be positive.");
        }

        WithdrawFunds command = new()
        {
            Amount = input.Amount,
        };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(input.SourceAccountId);
        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            return StepResult.Failed(result.ErrorCode ?? AggregateErrorCodes.InvalidCommand, result.ErrorMessage);
        }

        return StepResult.Succeeded();
    }
}