using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
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
    public IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection, TBrook>(
        string entityId
    )
        where TBrook : IBrookDefinition
    {
        UxProjectionKey key = UxProjectionKey.For<TProjection, TBrook>(entityId);
        return GetUxProjectionGrain<TProjection>(key);
    }

    /// <inheritdoc />
    public IUxProjectionGrain<TProjection> GetUxProjectionGrain<TProjection>(
        UxProjectionKey key
    )
    {
        Logger.ResolvingProjectionGrain(nameof(IUxProjectionGrain<TProjection>), key);
        return GrainFactory.GetGrain<IUxProjectionGrain<TProjection>>(key);
    }

    /// <inheritdoc />
    public IUxProjectionCursorGrain GetUxProjectionCursorGrain<TProjection, TBrook>(
        string entityId
    )
        where TBrook : IBrookDefinition
    {
        UxProjectionKey key = UxProjectionKey.For<TProjection, TBrook>(entityId);
        return GetUxProjectionCursorGrain(key);
    }

    /// <inheritdoc />
    public IUxProjectionCursorGrain GetUxProjectionCursorGrain(
        UxProjectionKey key
    )
    {
        Logger.ResolvingCursorGrain(nameof(IUxProjectionCursorGrain), key);
        return GrainFactory.GetGrain<IUxProjectionCursorGrain>(key);
    }
}
