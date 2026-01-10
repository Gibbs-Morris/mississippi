using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.L2Tests.Domain.CounterSummary;

/// <summary>
///     A read-optimized projection of counter aggregate state for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection demonstrates the "multiple projections per brook" pattern.
///         Both the CounterAggregate (the aggregate state) and this projection
///         derive from the same event stream (identified by <see cref="BrookNameAttribute" />), but serve
///         different purposes:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 CounterAggregate is the write-side aggregate state used for
///                 command processing and consistency enforcement.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="CounterSummaryProjection" /> is the read-side projection
///                 optimized for fast UX queries and display.
///             </description>
///         </item>
///     </list>
/// </remarks>
[BrookName("CRESCENT", "SAMPLE", "COUNTER")]
[SnapshotStorageName("CRESCENT", "SAMPLE", "COUNTERSUMMARY")]
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.CounterSummary.CounterSummaryProjection")]
internal sealed record CounterSummaryProjection
{
    /// <summary>
    ///     Gets the current count value.
    /// </summary>
    [Id(0)]
    public int CurrentCount { get; init; }

    /// <summary>
    ///     Gets a user-friendly display label summarizing the counter state.
    /// </summary>
    [Id(2)]
    public string DisplayLabel { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the counter is in a positive state.
    /// </summary>
    [Id(3)]
    public bool IsPositive { get; init; }

    /// <summary>
    ///     Gets the total number of operations performed on the counter.
    /// </summary>
    [Id(1)]
    public int TotalOperations { get; init; }
}