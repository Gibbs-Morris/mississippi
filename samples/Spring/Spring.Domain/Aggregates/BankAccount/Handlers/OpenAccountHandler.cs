using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for opening a bank account.
/// </summary>
internal sealed class OpenAccountHandler : CommandHandlerBase<OpenAccount, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        OpenAccount command,
        BankAccountAggregate? state
    )
    {
        // Prevent re-opening an already open account
        if (state?.IsOpen == true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.AlreadyExists,
                "Account is already open.");
        }

        // Validate holder name
        if (string.IsNullOrWhiteSpace(command.HolderName))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Account holder name is required.");
        }

        // Validate initial deposit is not negative
        if (command.InitialDeposit < 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Initial deposit cannot be negative.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new AccountOpened
                {
                    HolderName = command.HolderName,
                    InitialDeposit = command.InitialDeposit,
                },
            });
    }
}