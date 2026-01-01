using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.CounterSummary;

/// <summary>
///     A read-optimized projection of counter aggregate state for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection demonstrates the "multiple projections per brook" pattern.
///         Both <see cref="Counter.CounterAggregate" /> (the aggregate state) and this projection
///         derive from the same <see cref="Counter.CounterBrook" /> event stream, but serve
///         different purposes:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Counter.CounterAggregate" /> is the write-side aggregate state used for
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
[SnapshotStorageName("CRESCENT", "SAMPLE", "COUNTERSUMMARY")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.CounterSummary.CounterSummaryProjection")]
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