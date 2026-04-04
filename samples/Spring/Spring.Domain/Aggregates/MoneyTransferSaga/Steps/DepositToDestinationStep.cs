using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.BankAccount;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Commands;
using MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.Commands;


namespace MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.Steps;

/// <summary>
///     Saga step that deposits funds into the destination account.
/// </summary>
/// <remarks>
///     <para>
///         This step intentionally does not implement <see cref="ICompensatable{TSaga}" />
///         to demonstrate that saga steps can be non-compensatable. In a production scenario,
///         you might add compensation logic to withdraw the deposited funds if a subsequent
///         step fails. For this sample, the deposit is the final step so no compensation is needed.
///     </para>
/// </remarks>
[SagaStep<MoneyTransferSagaState>(1, SagaStepRecoveryPolicy.ManualOnly)]
internal sealed class DepositToDestinationStep : ISagaStep<MoneyTransferSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DepositToDestinationStep" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving bank account aggregates.</param>
    public DepositToDestinationStep(
        IAggregateGrainFactory aggregateGrainFactory
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        AggregateGrainFactory = aggregateGrainFactory;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        MoneyTransferSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidState, "Transfer input not provided.");
        }

        if (string.IsNullOrWhiteSpace(input.DestinationAccountId))
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand, "Destination account is required.");
        }

        if (input.Amount <= 0)
        {
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand, "Transfer amount must be positive.");
        }

        DepositFunds command = new()
        {
            Amount = input.Amount,
        };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(input.DestinationAccountId);
        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        if (!result.Success)
        {
            return StepResult.Failed(result.ErrorCode ?? AggregateErrorCodes.InvalidCommand, result.ErrorMessage);
        }

        return StepResult.Succeeded();
    }
}