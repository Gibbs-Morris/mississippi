using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots.Tests;

/// <summary>
///     Testable snapshot cache grain that exposes protected methods.
/// </summary>
[BrookName("TEST", "SNAPSHOTS", "TESTBROOK")]
internal sealed class TestableSnapshotCacheGrain : SnapshotCacheGrainBase<SnapshotCacheGrainTestState>
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
    /// <param name="retentionOptions">The snapshot retention options.</param>
    /// <param name="logger">The logger.</param>
    public TestableSnapshotCacheGrain(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<SnapshotCacheGrainTestState> rootReducer,
        ISnapshotStateConverter<SnapshotCacheGrainTestState> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        IOptions<SnapshotRetentionOptions> retentionOptions,
        ILogger logger
    )
        : base(
            grainContext,
            snapshotStorageReader,
            brookGrainFactory,
            rootReducer,
            snapshotStateConverter,
            snapshotGrainFactory,
            retentionOptions,
            logger)
    {
    }

    /// <summary>
    ///     Gets the brook name from the brook definition for testing.
    /// </summary>
    /// <returns>The brook name.</returns>
    public string GetBrookName() => BrookName;

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