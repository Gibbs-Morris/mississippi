using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Head;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Orleans grain implementation for reading a specific slice of a Mississippi brook (event stream).
///     Manages a portion of the brook's events in memory for efficient streaming and access.
/// </summary>
internal class BrookSliceReaderGrain
    : IBrookSliceReaderGrain,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookSliceReaderGrain" /> class.
    ///     Sets up the grain with required dependencies for brook slice reading operations.
    /// </summary>
    /// <param name="brookStorageReader">The brook storage reader service for accessing persisted events.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="brookGrainFactory">Factory for creating related brook grains.</param>
    public BrookSliceReaderGrain(
        IBrookStorageReader brookStorageReader,
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory
    )
    {
        BrookStorageReader = brookStorageReader;
        GrainContext = grainContext;
        BrookGrainFactory = brookGrainFactory;
    }

    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

    private IBrookStorageReader BrookStorageReader { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    /// <summary>
    ///     Reads events from this brook slice as an asynchronous stream within the specified position range.
    ///     Efficiently handles caching and streams events from the slice's managed range.
    /// </summary>
    /// <param name="minReadFrom">The minimum position to start reading events from.</param>
    /// <param name="maxReadTo">The maximum position to read events to.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous enumeration.</param>
    /// <returns>An asynchronous enumerable of brook events within the specified range from this slice.</returns>
    public async IAsyncEnumerable<BrookEvent> ReadAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        BrookRangeKey brookRangeKey = this.GetPrimaryKeyString();
        IBrookHeadGrain head = BrookGrainFactory.GetBrookHeadGrain(brookRangeKey.ToBrookCompositeKey());
        BrookPosition lastPositionOfBrook = await head.GetLatestPositionAsync();
        BrookPosition lastPositionOfSlice = brookRangeKey.End;
        BrookPosition lastPositionOfCache = Cache.Length + brookRangeKey.Start;
        if ((lastPositionOfCache < lastPositionOfSlice) && (lastPositionOfCache < lastPositionOfBrook))
        {
            await PopulateCacheFromBrookAsync(brookRangeKey);
        }

        for (int i = 0; i < Cache.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BrookPosition position = brookRangeKey.Start + i; // derive absolute position
            if (position < minReadFrom)
            {
                continue;
            }

            if (position > maxReadTo)
            {
                break; // cache is ordered, safe to stop
            }

            yield return Cache[i];
        }
    }

    /// <summary>
    ///     Reads events from this brook slice as a batch within the specified position range.
    ///     Returns all matching events as an immutable array for batch processing scenarios.
    /// </summary>
    /// <param name="minReadFrom">The minimum position to start reading events from.</param>
    /// <param name="maxReadTo">The maximum position to read events to.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>An immutable array containing all brook events within the specified range from this slice.</returns>
    public async Task<ImmutableArray<BrookEvent>> ReadBatchAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    )
    {
        List<BrookEvent> events = new();
        await foreach (BrookEvent ev in ReadAsync(minReadFrom, maxReadTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }

    /// <summary>
    ///     Deactivate and clear slice cache.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    public Task DeactivateAsync()
    {
        Cache = ImmutableArray<BrookEvent>.Empty;
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    private async Task PopulateCacheFromBrookAsync(
        BrookRangeKey brookRangeKey
    )
    {
        List<BrookEvent> l = new();
        await foreach (BrookEvent ev in BrookStorageReader.ReadEventsAsync(brookRangeKey))
        {
            l.Add(ev);
        }

        Cache = l.ToImmutableArray();
    }
}