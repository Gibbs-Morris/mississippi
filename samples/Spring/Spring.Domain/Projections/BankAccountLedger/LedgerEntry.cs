using Orleans;


namespace Spring.Domain.Projections.BankAccountLedger;

/// <summary>
///     A single entry in the account ledger.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountLedger.LedgerEntry")]
public sealed record LedgerEntry
{
    /// <summary>
    ///     Gets the amount in GBP.
    /// </summary>
    [Id(1)]
    public decimal AmountGbp { get; init; }

    /// <summary>
    ///     Gets the original amount in USD (only populated for USD deposits).
    /// </summary>
    [Id(2)]
    public decimal? AmountUsd { get; init; }

    /// <summary>
    ///     Gets the type of ledger entry.
    /// </summary>
    [Id(0)]
    public LedgerEntryType EntryType { get; init; }

    /// <summary>
    ///     Gets the exchange rate used (only populated for USD deposits).
    /// </summary>
    [Id(3)]
    public decimal? ExchangeRate { get; init; }

    /// <summary>
    ///     Gets the sequence number for ordering entries.
    /// </summary>
    [Id(4)]
    public long Sequence { get; init; }
}