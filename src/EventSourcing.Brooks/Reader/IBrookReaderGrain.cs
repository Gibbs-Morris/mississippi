using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Brooks.Reader;

/// <summary>
///     Main entry point grain for reading events from a brook (event stream) using batch operations.
///     This is a stateless worker grain that delegates to slice readers for actual event retrieval.
/// </summary>
/// <remarks>
///     <para>
///         This grain is a <c>[StatelessWorker]</c> for parallel batch read distribution.
///         For streaming reads using <c>IAsyncEnumerable</c>, use <see cref="IBrookAsyncReaderGrain" /> instead.
///     </para>
///     <para>
///         The async reader variant exists because Orleans' <c>IAsyncEnumerable</c> implementation
///         stores enumerator state in a per-activation extension, which is incompatible with
///         <c>[StatelessWorker]</c> routing.
///     </para>
/// </remarks>
[Alias("Mississippi.Core.IBrookReaderGrain")]
public interface IBrookReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Requests the reader grain to deactivate when idle.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    [Alias("DeactivateAsync")]
    Task DeactivateAsync();

    /// <summary>
    ///     Reads events from the brook and returns them as a batch collection.
    ///     Useful when you need to process all events at once or know the range is small.
    /// </summary>
    /// <param name="readFrom">The starting position to read from. If null, reads from the beginning.</param>
    /// <param name="readTo">The ending position to read to. If null, reads to the end.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An immutable array containing all brook events in the specified range.</returns>
    [ReadOnly]
    [Alias("ReadEventsBatchAsync")]
    Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );
}