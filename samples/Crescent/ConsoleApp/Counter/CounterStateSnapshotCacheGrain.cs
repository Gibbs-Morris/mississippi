using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Snapshot cache grain for <see cref="CounterState" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides versioned, immutable access to aggregate state snapshots.
///         It uses retention-based loading to efficiently rebuild state from the nearest
///         retained base snapshot plus a minimal delta of events.
///     </para>
///     <para>
///         The grain is keyed by <see cref="SnapshotKey" /> containing the snapshot name,
///         brook ID, reducers hash, and version.
///     </para>
/// </remarks>
internal sealed class CounterStateSnapshotCacheGrain
    : SnapshotCacheGrain<CounterState, CounterBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CounterStateSnapshotCacheGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotStorageReader">Reader for loading snapshots from storage.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="rootReducer">The root reducer for computing state from events.</param>
    /// <param name="snapshotStateConverter">Converter for serializing/deserializing state.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="retentionOptions">Options controlling snapshot retention and replay strategy.</param>
    /// <param name="brookEventConverter">Converter for deserializing brook events to domain events.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterStateSnapshotCacheGrain(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<CounterState> rootReducer,
        ISnapshotStateConverter<CounterState> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        IOptions<SnapshotRetentionOptions> retentionOptions,
        IBrookEventConverter brookEventConverter,
        ILogger<CounterStateSnapshotCacheGrain> logger
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
        BrookEventConverter = brookEventConverter;
    }

    private IBrookEventConverter BrookEventConverter { get; }

    /// <inheritdoc />
    protected override CounterState CreateInitialState() =>
        new()
        {
            Count = 0,
            DecrementCount = 0,
            IncrementCount = 0,
            IsInitialized = false,
            ResetCount = 0,
        };

    /// <inheritdoc />
    protected override object DeserializeEvent(BrookEvent brookEvent) =>
        BrookEventConverter.ToDomainEvent(brookEvent);
}
