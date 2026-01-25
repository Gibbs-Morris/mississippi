using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Handlers;

/// <summary>
///     Command handler for depositing dollars (USD) into a bank account.
/// </summary>
/// <remarks>
///     This handler validates the deposit and emits a <see cref="DollarsDeposited" />
///     event. The <c>CurrencyConversionEffect</c> then reacts to this event by
///     fetching the exchange rate and yielding a <see cref="ConvertedDollarsDeposited" />
///     event with the converted GBP amount.
/// </remarks>
internal sealed class DepositDollarsHandler : CommandHandlerBase<DepositDollars, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DepositDollars command,
        BankAccountAggregate? state
    )
    {
        // Account must be open to deposit funds
        if (state?.IsOpen != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Account must be open before depositing funds.");
        }

        // Validate deposit amount is positive
        if (command.AmountUsd <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Deposit amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new DollarsDeposited
                {
                    AmountUsd = command.AmountUsd,
                },
            });
    }
}