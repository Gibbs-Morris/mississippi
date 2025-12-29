using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Factory;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Orleans grain implementation for reading events from a brook using asynchronous streaming.
///     Each instance is uniquely identified and relies on Orleans' idle deactivation policy for cleanup.
/// </summary>
/// <remarks>
///     <para>
///         This grain is NOT a <c>[StatelessWorker]</c> because Orleans' IAsyncEnumerable implementation
///         stores enumerator state in a per-activation <c>AsyncEnumerableGrainExtension</c>. With StatelessWorker,
///         <c>MoveNextAsync()</c> calls could route to different activations, causing
///         <c>EnumerationAbortedException</c>.
///     </para>
///     <para>
///         To achieve parallelism, the <see cref="IBrookGrainFactory" /> creates unique grain keys
///         with random suffixes for each streaming request. After streaming completes, the grain
///         is eventually garbage-collected by Orleans' idle deactivation policy.
///     </para>
/// </remarks>
internal class BrookAsyncReaderGrain
    : IBrookAsyncReaderGrain,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookAsyncReaderGrain" /> class.
    ///     Sets up the grain with required dependencies for brook reading operations.
    /// </summary>
    /// <param name="brookGrainFactory">Factory for creating related brook grains.</param>
    /// <param name="options">Configuration options for brook reader behavior.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    public BrookAsyncReaderGrain(
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
    ///     Reads events from the brook as an asynchronous stream within the specified position range.
    ///     The grain deactivates itself after enumeration completes or is cancelled.
    /// </summary>
    /// <param name="readFrom">Optional starting position for reading events. Reads from beginning if null.</param>
    /// <param name="readTo">Optional ending position for reading events. Reads to end if null.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous enumeration.</param>
    /// <returns>An asynchronous enumerable of brook events within the specified range.</returns>
    /// <remarks>
    ///     This grain uses unique keys (via <see cref="BrookAsyncReaderKey" />) to ensure single-use semantics.
    ///     After enumeration completes, the grain will eventually be garbage-collected by Orleans'
    ///     idle deactivation policy. We do not call <c>DeactivateOnIdle()</c> here because async
    ///     iterators with <c>yield return</c> can pause between <c>MoveNextAsync</c> calls, causing
    ///     premature deactivation.
    /// </remarks>
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // Parse the brook key from the grain's primary key (strips the instance suffix)
        BrookAsyncReaderKey asyncReaderKey = this.GetPrimaryKeyString();
        BrookKey brookId = asyncReaderKey.BrookKey;
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
            yield break;
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
}