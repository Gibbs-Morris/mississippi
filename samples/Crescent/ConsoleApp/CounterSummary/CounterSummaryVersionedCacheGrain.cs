using Crescent.ConsoleApp.Counter;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.UxProjections;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.CounterSummary;

/// <summary>
///     Versioned UX projection cache grain for <see cref="CounterSummaryProjection" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides cached, versioned access to specific versions of
///         <see cref="CounterSummaryProjection" /> state. It is used by
///         <see cref="CounterSummaryProjectionGrain" /> to serve historical projections.
///     </para>
///     <para>
///         The grain is a stateless worker that caches a specific projection version
///         in memory. If the projection is not cached, it fetches from the underlying
///         snapshot cache grain.
///     </para>
/// </remarks>
internal sealed class CounterSummaryVersionedCacheGrain
    : UxProjectionVersionedCacheGrainBase<CounterSummaryProjection, CounterBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CounterSummaryVersionedCacheGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for computing the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterSummaryVersionedCacheGrain(
        IGrainContext grainContext,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<CounterSummaryProjection> rootReducer,
        ILogger<CounterSummaryVersionedCacheGrain> logger
    )
        : base(grainContext, snapshotGrainFactory, rootReducer.GetReducerHash(), logger)
    {
    }
}