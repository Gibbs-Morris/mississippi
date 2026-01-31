using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;


namespace Spring.Domain.Aggregates.TransactionInvestigationQueue.Handlers;

/// <summary>
///     Command handler for flagging high-value transactions for investigation.
/// </summary>
internal sealed class FlagTransactionHandler
    : CommandHandlerBase<FlagTransaction, TransactionInvestigationQueueAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagTransactionHandler" /> class
    ///     using system time.
    /// </summary>
    public FlagTransactionHandler()
        : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagTransactionHandler" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider for generating timestamps. Uses system time if null.</param>
    public FlagTransactionHandler(
        TimeProvider? timeProvider
    ) =>
        TimeProvider = timeProvider ?? TimeProvider.System;

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        FlagTransaction command,
        TransactionInvestigationQueueAggregate? state
    )
    {
        // Validate command has required data
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Account ID is required.");
        }

        if (command.Amount <= 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Amount must be positive.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new TransactionFlagged
                {
                    AccountId = command.AccountId,
                    Amount = command.Amount,
                    OriginalTimestamp = command.Timestamp,
                    FlaggedTimestamp = TimeProvider.GetUtcNow(),
                },
            });
    }
}