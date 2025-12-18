using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;


namespace Mississippi.EventSourcing.UxProjections.L0Tests.Infrastructure;

/// <summary>
///     In-memory implementation of <see cref="IBrookStorageReader" /> for testing purposes.
/// </summary>
internal sealed class InMemoryBrookStorageReader : IBrookStorageReader
{
    /// <inheritdoc />
    public Task<BrookPosition> ReadCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(new BrookPosition(-1));

    /// <inheritdoc />
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}