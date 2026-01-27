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
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the type of ledger entry.
    /// </summary>
    [Id(0)]
    public LedgerEntryType EntryType { get; init; }

    /// <summary>
    ///     Gets the sequence number for ordering entries.
    /// </summary>
    [Id(2)]
    public long Sequence { get; init; }
}