using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for refunding a failed transfer by restoring funds to the source account.
/// </summary>
internal sealed class RefundTransferHandler : CommandHandlerBase<RefundTransfer, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RefundTransfer command,
        BankAccountAggregate? state
    )
    {
        // Account must exist (may have been closed after transfer was initiated)
        if (state is null)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account not found for refund.");
        }

        // Validate refund amount is positive
        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Refund amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new FundsRefunded
                {
                    Amount = command.Amount,
                    TransferCorrelationId = command.TransferCorrelationId,
                    Reason = command.Reason,
                },
            });
    }
}