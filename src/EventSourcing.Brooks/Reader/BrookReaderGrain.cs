using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Factory;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Brooks.Reader;

/// <summary>
///     Orleans grain implementation for reading events from a Mississippi brook (event stream).
///     Serves as the main entry point for batch brook reading operations and delegates to slice readers.
/// </summary>
/// <remarks>
///     <para>
///         This grain is a <c>[StatelessWorker]</c> for parallel batch read distribution.
///         For streaming reads using <c>IAsyncEnumerable</c>, use <see cref="BrookAsyncReaderGrain" /> instead.
///     </para>
///     <para>
///         The async reader variant exists because Orleans' <c>IAsyncEnumerable</c> implementation
///         stores enumerator state in a per-activation <c>AsyncEnumerableGrainExtension</c>, which is
///         incompatible with <c>[StatelessWorker]</c> routing (MoveNext calls can route to wrong activation).
///     </para>
/// </remarks>
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
    /// <param name="logger">Logger instance for logging reader operations.</param>
    public BrookReaderGrain(
        IBrookGrainFactory brookGrainFactory,
        IOptions<BrookReaderOptions> options,
        IGrainContext grainContext,
        ILogger<BrookReaderGrain> logger
    )
    {
        BrookGrainFactory = brookGrainFactory;
        Options = options;
        GrainContext = grainContext;
        Logger = logger;
    }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    private ILogger<BrookReaderGrain> Logger { get; }

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
    ///     Deactivate the stateless reader grain.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    public Task DeactivateAsync()
    {
        BrookKey brookId = BrookKey.FromString(this.GetPrimaryKeyString());
        Logger.Deactivating(brookId);
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called when the grain is activated.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        BrookKey brookId = BrookKey.FromString(this.GetPrimaryKeyString());
        Logger.Activated(brookId);
        return Task.CompletedTask;
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
        BrookKey brookId = BrookKey.FromString(this.GetPrimaryKeyString());
        Logger.ReadingEventsBatch(brookId, readFrom?.Value, readTo?.Value);
        Stopwatch sw = Stopwatch.StartNew();
        BrookPosition start = readFrom ?? new BrookPosition(0);
        BrookPosition end;
        if (!readTo.HasValue)
        {
            IBrookCursorGrain cursorGrain = BrookGrainFactory.GetBrookCursorGrain(brookId);
            end = await cursorGrain.GetLatestPositionAsync();
        }
        else
        {
            end = readTo.Value;
        }

        // If the brook is empty (cursor returns -1) or end < start, there's nothing to read
        if ((end.Value < 0) || (end.Value < start.Value))
        {
            Logger.BrookEmpty(brookId);
            return ImmutableArray<BrookEvent>.Empty;
        }

        long sliceSize = Options.Value.BrookSliceSize;
        List<(long BucketId, long First, long Last)> baseIndexes = GetSliceReads(start.Value, end.Value, sliceSize);

        // Fan-out: kick off all slice batch reads in parallel
        List<Task<ImmutableArray<BrookEvent>>> sliceTasks = new(baseIndexes.Count);
        foreach ((long BucketId, long First, long Last) l in baseIndexes)
        {
            long sliceStart = l.First;
            long sliceCount = (l.Last - l.First) + 1;
            IBrookSliceReaderGrain sliceGrain = BrookGrainFactory.GetBrookSliceReaderGrain(
                BrookRangeKey.FromBrookCompositeKey(brookId, sliceStart, sliceCount));
            sliceTasks.Add(sliceGrain.ReadBatchAsync(l.First, l.Last, cancellationToken));
        }

        // Await all slices and concatenate in order
        ImmutableArray<BrookEvent>[] sliceResults = await Task.WhenAll(sliceTasks);
        List<BrookEvent> allEvents = new();
        foreach (ImmutableArray<BrookEvent> sliceEvents in sliceResults)
        {
            allEvents.AddRange(sliceEvents);
        }

        sw.Stop();
        Logger.EventsBatchRead(brookId, allEvents.Count, baseIndexes.Count, sw.ElapsedMilliseconds);
        return [..allEvents];
    }
}