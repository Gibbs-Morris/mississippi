using System.Collections.Generic;
using System.Threading;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Reader;

/// <summary>
///     Grain interface for reading events from a brook using asynchronous streaming.
///     This grain is designed for single-use: each instance has a unique key with a random suffix,
///     performs the streaming read, then deactivates itself when idle.
/// </summary>
/// <remarks>
///     <para>
///         This grain is NOT a <c>[StatelessWorker]</c> because Orleans' IAsyncEnumerable implementation
///         stores enumerator state in a per-activation <c>AsyncEnumerableGrainExtension</c>. StatelessWorker
///         allows multiple activations, which would cause <c>MoveNextAsync()</c> calls to route to different
///         activations than the one holding the enumerator, resulting in <c>EnumerationAbortedException</c>.
///     </para>
///     <para>
///         To achieve parallelism despite this limitation, each streaming read request gets a unique grain
///         instance (via a random suffix in the key). The grain deactivates itself after streaming completes,
///         keeping memory usage low.
///     </para>
///     <para>
///         For batch reads that don't require streaming, use <see cref="IBrookReaderGrain" /> which IS a
///         <c>[StatelessWorker]</c> and provides parallel batch access.
///     </para>
/// </remarks>
[Alias("Mississippi.Core.IBrookAsyncReaderGrain")]
public interface IBrookAsyncReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Reads events from the brook as an asynchronous stream.
    ///     Allows for efficient streaming of large numbers of events without loading them all into memory.
    ///     The grain will deactivate itself after enumeration completes or is cancelled.
    /// </summary>
    /// <param name="readFrom">The starting position to read from. If null, reads from the beginning.</param>
    /// <param name="readTo">The ending position to read to. If null, reads to the current cursor position.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of brook events in sequence order.</returns>
    [Alias("ReadEventsAsync")]
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );
}