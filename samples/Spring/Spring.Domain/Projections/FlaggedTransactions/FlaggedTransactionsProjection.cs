using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Inlet.Projection.Abstractions;

using Orleans;


namespace Spring.Domain.Projections.FlaggedTransactions;

/// <summary>
///     Read-optimized projection for the last 30 flagged transactions requiring investigation.
/// </summary>
/// <remarks>
///     <para>
///         This projection tracks high-value deposits that exceeded the AML threshold
///         and were flagged for manual investigation. It shows the most recent 30
///         flagged transactions, ordered by flagged timestamp descending.
///     </para>
///     <para>
///         <strong>Singleton Projection:</strong> This projection is for the global
///         <see cref="Aggregates.TransactionInvestigationQueue.TransactionInvestigationQueueAggregate" />
///         aggregate which collects flags from all bank accounts.
///     </para>
/// </remarks>
[ProjectionPath("flagged-transactions")]
[BrookName("SPRING", "COMPLIANCE", "INVESTIGATION")]
[SnapshotStorageName("SPRING", "COMPLIANCE", "FLAGGEDTXPROJECTION")]
[GenerateProjectionEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Projections.FlaggedTransactions.FlaggedTransactionsProjection")]
public sealed record FlaggedTransactionsProjection
{
    /// <summary>
    ///     The maximum number of flagged transactions to retain.
    /// </summary>
    public const int MaxEntries = 30;

    /// <summary>
    ///     Gets the current sequence number for ordering entries.
    /// </summary>
    [Id(1)]
    public long CurrentSequence { get; init; }

    /// <summary>
    ///     Gets the flagged transaction entries ordered by sequence descending (most recent first).
    /// </summary>
    [Id(0)]
    public ImmutableArray<FlaggedTransaction> Entries { get; init; } = [];
}