using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="CounterSummaryProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain demonstrates the "multiple projections per brook" pattern.
///         It consumes the same <see cref="CounterBrook" /> event stream as the
///         <see cref="CounterAggregateGrain" />, but produces a read-optimized
///         projection for UX display purposes.
///     </para>
///     <para>
///         The grain is a stateless worker that caches the last returned projection
///         in memory. On each request, it checks the cursor position and only fetches
///         a new snapshot if the brook has advanced since the last read.
///     </para>
/// </remarks>
internal sealed class CounterSummaryProjectionGrain
    : UxProjectionGrain<CounterSummaryProjection, CounterBrook>
{
    /// <summary>
    ///     Hash of the reducers used for snapshot key construction.
    ///     In production, this would be computed from the actual reducer configuration.
    /// </summary>
    private const string CounterReducersHash = "counter-summary-v1";

    /// <summary>
    ///     Initializes a new instance of the <see cref="CounterSummaryProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterSummaryProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ISnapshotGrainFactory snapshotGrainFactory,
        ILogger<CounterSummaryProjectionGrain> logger
    )
        : base(
            grainContext,
            uxProjectionGrainFactory,
            snapshotGrainFactory,
            CounterReducersHash,
            logger)
    {
    }
}
