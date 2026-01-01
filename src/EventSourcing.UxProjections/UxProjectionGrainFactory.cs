using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Factory for resolving UX projection grains by key.
/// </summary>
internal sealed class UxProjectionGrainFactory : IUxProjectionGrainFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionGrainFactory" /> class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory for creating grain instances.</param>
    /// <param name="logger">Logger instance for logging grain factory operations.</param>
    public UxProjectionGrainFactory(
        IGrainFactory grainFactory,
        ILogger<UxProjectionGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<UxProjectionGrainFactory> Logger { get; }

    /// <inheritdoc />
    public IUxProjectionCursorGrain GetUxProjectionCursorGrain(
        UxProjectionCursorKey key
    )
    {
        Logger.ResolvingCursorGrain(nameof(IUxProjectionCursorGrain), key);
        return GrainFactory.GetGrain<IUxProjectionCursorGrain>(key);
    }

    /// <inheritdoc />
    public IUxProjectionCursorGrain GetUxProjectionCursorGrainForGrain<TProjection, TGrain>(
        string entityId
    )
        where TGrain : class
    {
        string brookName = BrookNameHelper.GetBrookName<TGrain>();
        UxProjectionCursorKey key = new(brookName, entityId);
        return GetUxProjectionCursorGrain(key);
    }

    /// <inheritdoc />
    public IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(
        string entityId
    )
    {
        Logger.ResolvingProjectionGrain(nameof(IUxProjectionGrain<TProjection>), entityId);
        return GrainFactory.GetGrain<IUxProjectionGrain<TProjection>>(entityId);
    }

    /// <inheritdoc />
    public IUxProjectionVersionedCacheGrain<TProjection> GetUxProjectionVersionedCacheGrain<TProjection>(
        UxProjectionVersionedCacheKey key
    )
    {
        Logger.ResolvingVersionedCacheGrain(nameof(IUxProjectionVersionedCacheGrain<TProjection>), key);
        return GrainFactory.GetGrain<IUxProjectionVersionedCacheGrain<TProjection>>(key);
    }

    /// <inheritdoc />
    public IUxProjectionVersionedCacheGrain<TProjection>
        GetUxProjectionVersionedCacheGrainForGrain<TProjection, TGrain>(
            string entityId,
            BrookPosition version
        )
        where TGrain : class
    {
        string brookName = BrookNameHelper.GetBrookName<TGrain>();
        UxProjectionVersionedCacheKey key = new(brookName, entityId, version);
        return GetUxProjectionVersionedCacheGrain<TProjection>(key);
    }
}