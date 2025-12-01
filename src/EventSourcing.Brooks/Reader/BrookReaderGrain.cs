using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Factory;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Stateless Orleans grain implementation for reading events from a Mississippi brook (event stream).
///     Serves as the main entry point for brook reading operations and delegates to slice readers.
/// </summary>
[StatelessWorker]
internal class BrookReaderGrain
    : IBrookReaderGrain,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookReaderGrain" /> class.
    ///     Sets up the grain with required dependencies for brook reading operations.
    /// </summary>
    /// <param name="brookGrainFactory">Factory for creating related brook grains.</param>
    /// <param name="options">Configuration options for brook reader behavior.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    public BrookReaderGrain(
        IBrookGrainFactory brookGrainFactory,
        IOptions<BrookReaderOptions> options,
        IGrainContext grainContext
    )
    {
        BrookGrainFactory = brookGrainFactory;
        Options = options;
        GrainContext = grainContext;
    }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    private IOptions<BrookReaderOptions> Options { get; }

    private static List<(long BucketId, long First, long Last)> GetSliceReads(
        long start,
        long end,
        long sliceSize
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sliceSize);
        if (start > end)
        {
            throw new ArgumentException("start must be ≤ end");
        }

        long first = (long)Math.Floor((double)start / sliceSize) * sliceSize;
        long last = (long)Math.Floor((double)end / sliceSize) * sliceSize;
        List<(long BucketId, long First, long Last)> result = new();
        for (long b = first; b <= last; b += sliceSize)
        {
            long bucketId = b / sliceSize;
            long bucketFirst = Math.Max(b, start);
            long bucketLast = Math.Min((b + sliceSize) - 1, end);
            result.Add((bucketId, bucketFirst, bucketLast));
        }

        return result;
    }

    /// <summary>
    ///     Deactivate the stateless reader grain; this will allow any slice reader activations to expire.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    public Task DeactivateAsync()
    {
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads events from the brook as an asynchronous stream within the specified position range.
    ///     Delegates to slice reader grains for efficient memory management of large event streams.
    /// </summary>
    /// <param name="readFrom">Optional starting position for reading events. Reads from beginning if null.</param>
    /// <param name="readTo">Optional ending position for reading events. Reads to end if null.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous enumeration.</param>
    /// <returns>An asynchronous enumerable of brook events within the specified range.</returns>
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        BrookKey brookId = this.GetPrimaryKeyString();
        BrookPosition start = 0;
        BrookPosition end = 0;
        start = readFrom ?? new BrookPosition(0);
        if (!readTo.HasValue)
        {
            IBrookCursorGrain cursorGrain = BrookGrainFactory.GetBrookCursorGrain(brookId);
            end = await cursorGrain.GetLatestPositionAsync();
        }
        else
        {
            end = readTo.Value;
        }

        long sliceSize = Options.Value.BrookSliceSize;
        List<(long BucketId, long First, long Last)> baseIndexes = GetSliceReads(start.Value, end.Value, sliceSize);
        foreach ((long BucketId, long First, long Last) l in baseIndexes)
        {
            // Construct slice key using the actual slice start and an inclusive end via Count=(Last-First)
            long sliceStart = l.First;
            long sliceCount = (l.Last - l.First) + 1; // Count is inclusive so Start + Count - 1 == l.Last
            IBrookSliceReaderGrain sliceGrain = BrookGrainFactory.GetBrookSliceReaderGrain(
                BrookRangeKey.FromBrookCompositeKey(brookId, sliceStart, sliceCount));
            await foreach (BrookEvent mississippiEvent in sliceGrain.ReadAsync(l.First, l.Last, cancellationToken))
            {
                yield return mississippiEvent;
            }
        }
    }

    /// <summary>
    ///     Reads events from the brook as a batch within the specified position range.
    ///     Returns all matching events as an immutable array for batch processing scenarios.
    /// </summary>
    /// <param name="readFrom">Optional starting position for reading events. Reads from beginning if null.</param>
    /// <param name="readTo">Optional ending position for reading events. Reads to end if null.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>An immutable array containing all brook events within the specified range.</returns>
    public async Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    )
    {
        List<BrookEvent> events = new();
        await foreach (BrookEvent ev in ReadEventsAsync(readFrom, readTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }
}