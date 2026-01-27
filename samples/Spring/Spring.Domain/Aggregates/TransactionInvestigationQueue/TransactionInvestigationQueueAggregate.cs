using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.TransactionInvestigationQueue;

/// <summary>
///     Aggregate that maintains a queue of high-value transactions flagged for manual investigation.
/// </summary>
/// <remarks>
///     <para>
///         This aggregate is used as a singleton, receiving cross-aggregate commands from effects
///         that trigger on high-value deposits in individual bank accounts. For AML compliance
///         demonstration, any deposit exceeding £10,000 is flagged for investigation.
///     </para>
///     <para>
///         <strong>Bottleneck Warning:</strong> A singleton aggregate creates a natural bottleneck
///         as all flagging commands for all accounts converge here. In production, consider sharding
///         by time period, region, or other dimension.
///     </para>
/// </remarks>
[BrookName("SPRING", "COMPLIANCE", "INVESTIGATION")]
[SnapshotStorageName("SPRING", "COMPLIANCE", "INVESTIGATIONSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.TransactionInvestigationQueue.TransactionInvestigationQueueAggregate")]
public sealed record TransactionInvestigationQueueAggregate
{
    /// <summary>
    ///     Gets the total number of transactions flagged since aggregate creation.
    /// </summary>
    [Id(0)]
    public int TotalFlaggedCount { get; init; }
}