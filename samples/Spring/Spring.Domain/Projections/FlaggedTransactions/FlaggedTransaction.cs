using System;

using Orleans;


namespace Spring.Domain.Projections.FlaggedTransactions;

/// <summary>
///     A single entry in the flagged transactions list.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Projections.FlaggedTransactions.FlaggedTransaction")]
public sealed record FlaggedTransaction
{
    /// <summary>
    ///     Gets the identifier of the bank account that received the flagged deposit.
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

    /// <summary>
    ///     Gets the sequence number for ordering entries.
    /// </summary>
    [Id(4)]
    public long Sequence { get; init; }
}