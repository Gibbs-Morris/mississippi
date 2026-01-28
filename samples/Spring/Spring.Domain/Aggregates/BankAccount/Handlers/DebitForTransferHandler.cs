using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for debiting funds from a bank account during an outgoing transfer.
/// </summary>
internal sealed class DebitForTransferHandler : CommandHandlerBase<DebitForTransfer, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DebitForTransfer command,
        BankAccountAggregate? state
    )
    {
        // Account must be open to transfer
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open before transferring funds.");
        }

        // Validate transfer amount is positive
        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Transfer amount must be positive.");
        }

        // Validate sufficient funds
        if (state.Balance < command.Amount)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Insufficient funds for transfer.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new FundsDebited
                {
                    Amount = command.Amount,
                    TransferCorrelationId = command.TransferCorrelationId,
                    DestinationAccountId = command.DestinationAccountId,
                },
            });
    }
}