using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for depositing cash into a bank account.
/// </summary>
internal sealed class DepositCashHandler : CommandHandlerBase<DepositCash, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DepositCash command,
        BankAccountAggregate? state
    )
    {
        // Account must be open to deposit cash
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open before depositing cash.");
        }

        // Validate deposit amount is positive
        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Deposit amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new CashDeposited
                {
                    Amount = command.Amount,
                },
            });
    }
}