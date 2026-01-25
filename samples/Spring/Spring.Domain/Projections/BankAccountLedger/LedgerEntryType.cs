using Orleans;


namespace Spring.Domain.Projections.BankAccountLedger;

/// <summary>
///     Identifies the type of ledger entry.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountLedger.LedgerEntryType")]
public enum LedgerEntryType
{
    /// <summary>
    ///     A deposit in GBP.
    /// </summary>
    DepositGbp,

    /// <summary>
    ///     A deposit originally in USD, converted to GBP.
    /// </summary>
    DepositUsd,

    /// <summary>
    ///     A withdrawal.
    /// </summary>
    Withdrawal,
}