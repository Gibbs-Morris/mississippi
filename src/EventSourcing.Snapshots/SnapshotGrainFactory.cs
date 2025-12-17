using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     Factory for resolving Orleans snapshot grains (cache and persister) by key.
/// </summary>
internal sealed class SnapshotGrainFactory : ISnapshotGrainFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotGrainFactory" /> class.
    ///     Sets up the factory with Orleans grain factory and logging dependencies.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory for creating grain instances.</param>
    /// <param name="logger">Logger instance for logging grain factory operations.</param>
    public SnapshotGrainFactory(
        IGrainFactory grainFactory,
        ILogger<SnapshotGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the Orleans grain factory.
    /// </summary>
    private IGrainFactory GrainFactory { get; }

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    private ILogger<SnapshotGrainFactory> Logger { get; }

    /// <inheritdoc />
    public ISnapshotCacheGrain<TState> GetSnapshotCacheGrain<TState>(
        SnapshotKey snapshotKey
    )
    {
        Logger.ResolvingCacheGrain(nameof(ISnapshotCacheGrain<TState>), snapshotKey);
        return GrainFactory.GetGrain<ISnapshotCacheGrain<TState>>(snapshotKey);
    }

    /// <inheritdoc />
    public ISnapshotPersisterGrain GetSnapshotPersisterGrain(
        SnapshotKey snapshotKey
    )
    {
        Logger.ResolvingPersisterGrain(nameof(ISnapshotPersisterGrain), snapshotKey);
        return GrainFactory.GetGrain<ISnapshotPersisterGrain>(snapshotKey);
    }
}