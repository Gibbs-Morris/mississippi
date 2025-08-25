using System.Runtime.CompilerServices;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;


namespace Mississippi.EventSourcing.Tests.Infrastructure;

/// <summary>
///     Simple in-memory implementation of IBrookStorageReader/Writer for tests.
/// </summary>
internal sealed class InMemoryBrookStorage
    : IBrookStorageReader,
      IBrookStorageWriter
{
    private readonly Dictionary<string, List<BrookEvent>> eventsByKey = new();

    private readonly Dictionary<string, long> heads = new();

    public Task<BrookPosition> ReadHeadPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        _ = heads.TryGetValue(brookId, out long v);
        return Task.FromResult(new BrookPosition(v));
    }

    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        string key = brookRange.ToBrookCompositeKey();
        if (eventsByKey.TryGetValue(key, out List<BrookEvent>? list))
        {
            long start = brookRange.Start;
            long end = brookRange.End;
            for (int i = (int)start; (i < list.Count) && (i <= end); i++)
            {
                yield return list[i];
            }
        }

        await Task.CompletedTask;
    }

    public Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        string key = brookId;
        if (!eventsByKey.TryGetValue(key, out List<BrookEvent>? list))
        {
            list = new();
            eventsByKey[key] = list;
        }

        long head = heads.TryGetValue(key, out long v) ? v : 0;
        if (expectedVersion.HasValue && (expectedVersion.Value.Value != head))
        {
            throw new InvalidOperationException("expectedVersion mismatch");
        }

        list.AddRange(events);
        head += events.Count;
        heads[key] = head;
        return Task.FromResult(new BrookPosition(head));
    }

    public void SetHead(
        BrookKey brookId,
        long position
    ) =>
        heads[(string)brookId] = position;
}