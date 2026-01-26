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
    ///     A deposit.
    /// </summary>
    Deposit,

    /// <summary>
    ///     A withdrawal.
    /// </summary>
    Withdrawal,
}