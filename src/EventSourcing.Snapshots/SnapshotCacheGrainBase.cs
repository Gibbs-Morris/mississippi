using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     Base class for snapshot cache grains that provide immutable, versioned state access.
/// </summary>
/// <typeparam name="TSnapshot">The type of state stored in the snapshot.</typeparam>
/// <remarks>
///     <para>
///         Snapshot cache grains are keyed by <see cref="SnapshotKey" /> in the format
///         "projectionType|projectionId|reducersHash|version". Once activated and hydrated,
///         the state is immutable and cached in memory for fast read access.
///     </para>
///     <para>
///         On activation, the grain uses a retention-based strategy for efficient state building:
///         <list type="number">
///             <item>First attempts to load the exact snapshot from storage.</item>
///             <item>
///                 If not found, calculates the nearest retained base snapshot (using
///                 <see cref="SnapshotRetentionOptions" />).
///             </item>
///             <item>Gets state from the base snapshot grain and replays only the delta events.</item>
///         </list>
///     </para>
///     <para>
///         For example, with a modulus of 100, requesting state at version 364:
///         <list type="bullet">
///             <item>Base snapshot at version 300 is retrieved (or built recursively).</item>
///             <item>Only events 301-364 (64 events) are replayed.</item>
///         </list>
///     </para>
///     <para>
///         After state is built (from storage or rebuilt), a one-way call is made to an
///         <see cref="ISnapshotPersisterGrain" /> for background persistence if the snapshot
///         was rebuilt.
///     </para>
///     <para>
///         Derived classes MUST be decorated with <see cref="BrookNameAttribute" /> directly
///         on the final sealed class. If the attribute is missing, the grain will fail to
///         activate with an exception.
///     </para>
/// </remarks>
public abstract class SnapshotCacheGrainBase<TSnapshot>
    : ISnapshotCacheGrain<TSnapshot>,
      IGrainBase
{
    private SnapshotKey snapshotKey;

    private TSnapshot? state;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCacheGrainBase{TSnapshot}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotStorageReader">Reader for loading snapshots from storage.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="rootReducer">The root reducer for computing state from events.</param>
    /// <param name="snapshotStateConverter">Converter for serializing/deserializing state to/from envelopes.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="retentionOptions">Options controlling snapshot retention and replay strategy.</param>
    /// <param name="logger">Logger instance.</param>
    protected SnapshotCacheGrainBase(
        IGrainContext grainContext,
        ISnapshotStorageReader snapshotStorageReader,
        IBrookGrainFactory brookGrainFactory,
        IRootReducer<TSnapshot> rootReducer,
        ISnapshotStateConverter<TSnapshot> snapshotStateConverter,
        ISnapshotGrainFactory snapshotGrainFactory,
        IOptions<SnapshotRetentionOptions> retentionOptions,
        ILogger logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        SnapshotStorageReader = snapshotStorageReader ?? throw new ArgumentNullException(nameof(snapshotStorageReader));
        BrookGrainFactory = brookGrainFactory ?? throw new ArgumentNullException(nameof(brookGrainFactory));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        SnapshotStateConverter =
            snapshotStateConverter ?? throw new ArgumentNullException(nameof(snapshotStateConverter));
        SnapshotGrainFactory = snapshotGrainFactory ?? throw new ArgumentNullException(nameof(snapshotGrainFactory));
        RetentionOptions = retentionOptions?.Value ?? throw new ArgumentNullException(nameof(retentionOptions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the factory for resolving brook grains.
    /// </summary>
    protected IBrookGrainFactory BrookGrainFactory { get; }

    /// <summary>
    ///     Gets the brook name from the <see cref="BrookNameAttribute" /> on this grain type.
    /// </summary>
    /// <remarks>
    ///     This property reads the attribute from the concrete grain type at runtime.
    ///     If the attribute is missing, an <see cref="InvalidOperationException" /> is thrown.
    /// </remarks>
    protected string BrookName => BrookDefinitionHelper.GetBrookNameFromGrain(GetType());

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Gets the snapshot retention options controlling replay strategy.
    /// </summary>
    protected SnapshotRetentionOptions RetentionOptions { get; }

    /// <summary>
    ///     Gets the root reducer for computing state from events.
    /// </summary>
    protected IRootReducer<TSnapshot> RootReducer { get; }

    /// <summary>
    ///     Gets the factory for resolving snapshot grains.
    /// </summary>
    protected ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <summary>
    ///     Gets the converter for serializing/deserializing state to/from envelopes.
    /// </summary>
    protected ISnapshotStateConverter<TSnapshot> SnapshotStateConverter { get; }

    /// <summary>
    ///     Gets the reader for loading snapshots from storage.
    /// </summary>
    protected ISnapshotStorageReader SnapshotStorageReader { get; }

    /// <inheritdoc />
    public ValueTask<TSnapshot> GetStateAsync(
        CancellationToken cancellationToken = default
    ) =>
        new(state!);

    /// <summary>
    ///     Called when the grain is activated. Hydrates state from storage or rebuilds from the event stream.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <see cref="BrookNameAttribute" /> is missing from the concrete grain type.
    /// </exception>
    public virtual async Task OnActivateAsync(
        CancellationToken token
    )
    {
        // Validate that the attribute is present on the concrete grain type (fail-fast)
        // This call will throw InvalidOperationException if the attribute is missing
        _ = BrookDefinitionHelper.GetDefinition(GetType());
        string primaryKey = this.GetPrimaryKeyString();
        snapshotKey = SnapshotKey.FromString(primaryKey);
        Logger.Activating(primaryKey);
        string currentReducerHash = RootReducer.GetReducerHash();
        SnapshotEnvelope? envelope = await SnapshotStorageReader.ReadAsync(snapshotKey, token);
        if (envelope is not null)
        {
            // Check if reducer hash matches
            if (!string.IsNullOrEmpty(envelope.ReducerHash) &&
                string.Equals(envelope.ReducerHash, currentReducerHash, StringComparison.Ordinal))
            {
                // Snapshot is valid, use it directly
                state = SnapshotStateConverter.FromEnvelope(envelope);
                Logger.SnapshotLoadedFromStorage(primaryKey);
                Logger.Activated(primaryKey);
                return;
            }

            // Reducer hash mismatch - need to rebuild
            Logger.ReducerHashMismatch(primaryKey, envelope.ReducerHash, currentReducerHash);
        }
        else
        {
            Logger.NoSnapshotInStorage(primaryKey);
        }

        // Rebuild state from the event stream
        await RebuildStateFromStreamAsync(token);

        // Request background persistence since we rebuilt
        RequestBackgroundPersistence(currentReducerHash);
        Logger.Activated(primaryKey);
    }

    /// <summary>
    ///     Creates the initial state when no events exist in the stream.
    /// </summary>
    /// <returns>The initial state.</returns>
    protected abstract TSnapshot CreateInitialState();

    /// <summary>
    ///     Deserializes a brook event to a domain event object.
    /// </summary>
    /// <param name="brookEvent">The brook event to deserialize.</param>
    /// <returns>The deserialized domain event.</returns>
    /// <remarks>
    ///     Override this method to provide custom event deserialization logic.
    ///     The default implementation throws <see cref="NotImplementedException" />.
    /// </remarks>
    protected abstract object DeserializeEvent(
        BrookEvent brookEvent
    );

    /// <summary>
    ///     Gets the entity ID portion of the snapshot key for constructing the brook key.
    /// </summary>
    /// <returns>The entity ID.</returns>
    protected virtual string GetEntityId() => snapshotKey.Stream.ProjectionId;

    private async Task RebuildStateFromStreamAsync(
        CancellationToken token
    )
    {
        // Construct the brook key from the snapshot stream key
        BrookKey brookKey = new(BrookName, GetEntityId());
        string keyString = snapshotKey;
        Logger.RebuildingFromStream(brookKey, keyString);
        long targetVersion = snapshotKey.Version;
        long baseVersion = RetentionOptions.GetBaseSnapshotVersion<TSnapshot>(targetVersion);
        BrookPosition readFrom;
        if (baseVersion > 0)
        {
            // We have a base snapshot to build from
            long deltaEvents = targetVersion - baseVersion;
            Logger.UsingBaseSnapshot(baseVersion, targetVersion, deltaEvents);

            // Get state from the base snapshot
            SnapshotKey baseSnapshotKey = new(snapshotKey.Stream, baseVersion);
            ISnapshotCacheGrain<TSnapshot> baseSnapshotGrain =
                SnapshotGrainFactory.GetSnapshotCacheGrain<TSnapshot>(baseSnapshotKey);
            state = await baseSnapshotGrain.GetStateAsync(token);

            // Read events starting after the base version
            readFrom = new(baseVersion + 1);
        }
        else
        {
            // No base snapshot available, rebuild from the beginning
            Logger.NoBaseSnapshotAvailable(keyString);
            state = CreateInitialState();
            readFrom = new(0);
        }

        long eventCount = 0;

        // Read events up to the snapshot version using the async reader grain
        // (which is designed for streaming and auto-deactivates after use)
        BrookPosition readTo = new(targetVersion);
        IBrookAsyncReaderGrain readerGrain = BrookGrainFactory.GetBrookAsyncReaderGrain(brookKey);
        await foreach (BrookEvent brookEvent in readerGrain.ReadEventsAsync(readFrom, readTo, token)
                           .WithCancellation(token))
        {
            // The brook event data needs to be deserialized by the derived class
            // since we don't have access to the event converter here
            object eventData = DeserializeEvent(brookEvent);
            state = RootReducer.Reduce(state, eventData);
            eventCount++;
        }

        Logger.StateRebuilt(eventCount, keyString);
    }

    private void RequestBackgroundPersistence(
        string reducerHash
    )
    {
        if (state is null)
        {
            return;
        }

        string keyString = snapshotKey;
        Logger.RequestingPersistence(keyString);
        SnapshotEnvelope envelope = SnapshotStateConverter.ToEnvelope(state, reducerHash);
        ISnapshotPersisterGrain persisterGrain = SnapshotGrainFactory.GetSnapshotPersisterGrain(snapshotKey);

        // Fire-and-forget: method is marked with [OneWay] for background persistence
        _ = persisterGrain.PersistAsync(envelope);
    }
}