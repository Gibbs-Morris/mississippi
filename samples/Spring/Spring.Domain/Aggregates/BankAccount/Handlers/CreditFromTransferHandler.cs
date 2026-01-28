using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for crediting funds to a bank account during an incoming transfer.
/// </summary>
internal sealed class CreditFromTransferHandler : CommandHandlerBase<CreditFromTransfer, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        CreditFromTransfer command,
        BankAccountAggregate? state
    )
    {
        // Account must be open to receive transfer
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open to receive transfer.");
        }

        // Validate transfer amount is positive
        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Transfer amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new FundsReceived
                {
                    Amount = command.Amount,
                    TransferCorrelationId = command.TransferCorrelationId,
                    SourceAccountId = command.SourceAccountId,
                },
            });
    }
}