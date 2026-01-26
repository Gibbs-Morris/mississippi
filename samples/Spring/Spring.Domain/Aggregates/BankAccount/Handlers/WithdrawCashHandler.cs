using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for withdrawing cash from a bank account.
/// </summary>
internal sealed class WithdrawCashHandler : CommandHandlerBase<WithdrawCash, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        WithdrawCash command,
        BankAccountAggregate? state
    )
    {
        // Account must be open to withdraw cash
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open before withdrawing cash.");
        }

        // Validate withdrawal amount is positive
        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Withdrawal amount must be positive.");
        }

        // Validate sufficient funds
        if (state.Balance < command.Amount)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Insufficient funds for withdrawal.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CashWithdrawn
                {
                    Amount = command.Amount,
                },
            });
    }
}