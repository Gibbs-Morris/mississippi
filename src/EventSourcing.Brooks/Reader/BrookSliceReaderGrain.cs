using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Orleans grain implementation for reading a specific slice of a Mississippi brook (event stream).
///     Manages a portion of the brook's events in memory for efficient streaming and access.
/// </summary>
internal sealed class BrookSliceReaderGrain
    : IBrookSliceReaderGrain,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookSliceReaderGrain" /> class.
    ///     Sets up the grain with required dependencies for brook slice reading operations.
    /// </summary>
    /// <param name="brookStorageReader">The brook storage reader service for accessing persisted events.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BrookSliceReaderGrain(
        IBrookStorageReader brookStorageReader,
        IGrainContext grainContext,
        ILogger<BrookSliceReaderGrain> logger
    )
    {
        BrookStorageReader = brookStorageReader;
        GrainContext = grainContext;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    private IBrookStorageReader BrookStorageReader { get; }

    private ILogger<BrookSliceReaderGrain> Logger { get; }

    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

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
    ///     Called when the grain is activated. Populates the cache from storage.
    /// </summary>
    /// <param name="token">Cancellation token for the activation operation.</param>
    /// <returns>A task representing the asynchronous activation operation.</returns>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        BrookRangeKey brookRangeKey = this.GetPrimaryKeyString();
        Logger.SliceGrainActivating(brookRangeKey);
        await PopulateCacheFromBrookAsync(brookRangeKey, token);
        Logger.SliceCachePopulated(brookRangeKey, Cache.Length);
    }

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
        long lastPositionOfCacheValue = Cache.Length == 0
            ? brookRangeKey.Start.Value - 1
            : (brookRangeKey.Start.Value + Cache.Length) - 1;
        BrookPosition lastPositionOfCache = BrookPosition.FromLong(lastPositionOfCacheValue);

        // Validate that the requested range is covered by the cache.
        // The cache is populated on activation, so any request outside the cache is an error.
        if (maxReadTo > lastPositionOfCache)
        {
            throw new InvalidOperationException(
                $"Requested position {maxReadTo} exceeds cached range. " +
                $"Cache covers up to position {lastPositionOfCache} for slice {brookRangeKey}.");
        }

        // Yield control to ensure async semantics for the iterator.
        await Task.CompletedTask;
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

    private async Task PopulateCacheFromBrookAsync(
        BrookRangeKey brookRangeKey,
        CancellationToken cancellationToken
    )
    {
        List<BrookEvent> l = new();
        await foreach (BrookEvent ev in BrookStorageReader.ReadEventsAsync(brookRangeKey, cancellationToken))
        {
            l.Add(ev);
        }

        Cache = [.. l];
    }
}