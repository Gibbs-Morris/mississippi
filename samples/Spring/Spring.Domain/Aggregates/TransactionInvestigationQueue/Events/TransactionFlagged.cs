using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;

/// <summary>
///     Event raised when a high-value transaction is flagged for investigation.
/// </summary>
[EventStorageName("SPRING", "COMPLIANCE", "TRANSACTIONFLAGGED")]
[GenerateSerializer]
[Alias("Spring.Domain.TransactionInvestigationQueue.Events.TransactionFlagged")]
internal sealed record TransactionFlagged
{
    /// <summary>
    ///     Gets the identifier of the bank account that received the deposit.
    /// </summary>
    [Id(0)]
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the amount that was deposited.
    /// </summary>
    [Id(1)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the timestamp when the transaction was flagged for investigation.
    /// </summary>
    [Id(3)]
    public DateTimeOffset FlaggedTimestamp { get; init; }

    /// <summary>
    ///     Gets the timestamp when the original deposit occurred.
    /// </summary>
    [Id(2)]
    public DateTimeOffset OriginalTimestamp { get; init; }
}