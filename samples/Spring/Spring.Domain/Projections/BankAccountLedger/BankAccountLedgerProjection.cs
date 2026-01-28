using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Projections.BankAccountLedger;

/// <summary>
///     Read-optimized projection for the last 20 ledger entries of a bank account.
/// </summary>
/// <remarks>
///     <para>
///         This projection tracks the most recent deposits and withdrawals,
///         showing both GBP amounts and original USD amounts with exchange rates
///         for currency-converted deposits.
///     </para>
/// </remarks>
[ProjectionPath("bank-account-ledger")]
[BrookName("SPRING", "BANKING", "ACCOUNT")]
[SnapshotStorageName("SPRING", "BANKING", "ACCOUNTLEDGER")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Projections.BankAccountLedger.BankAccountLedgerProjection")]
public sealed record BankAccountLedgerProjection
{
    /// <summary>
    ///     The maximum number of entries to retain in the ledger.
    /// </summary>
    public const int MaxEntries = 20;

    /// <summary>
    ///     Gets the current sequence number for ordering entries.
    /// </summary>
    [Id(1)]
    public long CurrentSequence { get; init; }

    /// <summary>
    ///     Gets the ledger entries ordered by sequence descending (most recent first).
    /// </summary>
    [Id(0)]
    public ImmutableArray<LedgerEntry> Entries { get; init; } = [];
}