using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Testable snapshot cache grain that exposes protected methods.
/// </summary>
internal sealed class TestableSnapshotCacheGrain
    : SnapshotCacheGrain<SnapshotCacheGrainTestState, SnapshotCacheGrainTestBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestableSnapshotCacheGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="snapshotStorageReader">The snapshot storage reader.</param>
    /// <param name="brookGrainFactory">The brook grain factory.</param>
    /// <param name="rootReducer">The root reducer.</param>
    /// <param name="snapshotStateConverter">The snapshot state converter.</param>
    /// <param name="snapshotGrainFactory">The snapshot grain factory.</param>
    /// <param name="logger">The logger.</param>
    public TestableSnapshotCacheGrain(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<SnapshotCacheGrainTestState> rootReducer,
        ISnapshotStateConverter<SnapshotCacheGrainTestState> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        ILogger logger
    )
        : base(
            grainContext,
            snapshotStorageReader,
            brookGrainFactory,
            rootReducer,
            snapshotStateConverter,
            snapshotGrainFactory,
            logger)
    {
    }

    /// <inheritdoc />
    protected override SnapshotCacheGrainTestState CreateInitialState() =>
        new()
        {
            Value = 0,
        };

    /// <inheritdoc />
    protected override object DeserializeEvent(
        BrookEvent brookEvent
    ) =>
        throw new NotImplementedException("Test does not use event deserialization");
}