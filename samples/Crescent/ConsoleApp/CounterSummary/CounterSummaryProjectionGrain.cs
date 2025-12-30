using Crescent.ConsoleApp.Counter;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.CounterSummary;

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
internal sealed class CounterSummaryProjectionGrain : UxProjectionGrainBase<CounterSummaryProjection, CounterBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CounterSummaryProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterSummaryProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<CounterSummaryProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}