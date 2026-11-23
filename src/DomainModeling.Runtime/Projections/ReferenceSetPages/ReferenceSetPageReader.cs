using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;

using Orleans;


namespace Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

/// <summary>Coordinates one cursor capture and an immutable bounded cache read.</summary>
/// <typeparam name="TProjection">The named membership projection.</typeparam>
/// <typeparam name="TSource">The referenced source projection type.</typeparam>
internal sealed class ReferenceSetPageReader<TProjection, TSource> : IReferenceSetPageReader<TProjection, TSource>
    where TProjection : class
{
    /// <summary>Initializes a new instance of the <see cref="ReferenceSetPageReader{TProjection, TSource}" /> class.</summary>
    /// <param name="projections">The existing projection factory.</param>
    /// <param name="grains">The Orleans proxy factory.</param>
    /// <param name="logger">The typed logger.</param>
    public ReferenceSetPageReader(
        IUxProjectionGrainFactory projections,
        IGrainFactory grains,
        ILogger<ReferenceSetPageReader<TProjection, TSource>> logger
    )
    {
        Projections = projections ?? throw new ArgumentNullException(nameof(projections));
        Grains = grains ?? throw new ArgumentNullException(nameof(grains));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory Grains { get; }

    private ILogger Logger { get; }

    private IUxProjectionGrainFactory Projections { get; }

    /// <inheritdoc />
    public async ValueTask<ReferenceSetPage<TProjection, TSource>> ReadAsync(
        string entityId,
        int offset = 0,
        int limit = 50,
        BrookPosition? version = null,
        CancellationToken cancellationToken = default
    )
    {
        ReferenceSetPageSelection.Validate(offset, limit);
        cancellationToken.ThrowIfCancellationRequested();
        UxProjectionCursorKey cursorKey = new(BrookNameHelper.GetBrookName<TProjection>(), entityId);
        string entityHash = ReferenceSetPageLoggerExtensions.GetEntityHash(entityId);
        Stopwatch timer = Stopwatch.StartNew();
        long? requestedVersion = version.HasValue ? version.GetValueOrDefault().Value : null;
        Logger.PageReadStarted(entityHash, typeof(TProjection).Name, offset, limit, requestedVersion);
        BrookPosition selected = version.GetValueOrDefault();
        if (!version.HasValue)
        {
            IUxProjectionCursorGrain cursor = Projections.GetUxProjectionCursorGrain(cursorKey);
            selected = await cursor.GetPositionAsync().WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(selected.Value, -1);
        ReferenceSetPage<TProjection, TSource> result = selected.NotSet
            ? new()
            {
                Offset = offset,
                Limit = limit,
                TotalCount = 0,
                Version = selected,
                References = [],
            }
            : await Grains.GetGrain<IReferenceSetPageCacheGrain<TProjection, TSource>>(
                    UxProjectionVersionedCacheKey.FromCursorKey(cursorKey, selected))
                .ReadAsync(offset, limit, cancellationToken);
        Logger.PageReadCompleted(
            entityHash,
            result.Version.Value,
            result.References.Length,
            timer.Elapsed.TotalMilliseconds);
        return result;
    }
}