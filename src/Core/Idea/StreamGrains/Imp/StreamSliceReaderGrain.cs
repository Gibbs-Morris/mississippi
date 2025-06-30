using System.Collections.Immutable;

using Mississippi.Core.Idea.Storage;
using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.StreamGrains.Imp;

public class StreamSliceReaderGrain : IStreamSliceReaderGrain
{
    public StreamSliceReaderGrain(
        IStreamReaderService streamReaderService
    ) =>
        StreamReaderService = streamReaderService;

    private ImmutableDictionary<long, MississippiEvent> Cache { get; set; } =
        ImmutableDictionary<long, MississippiEvent>.Empty;

    private IStreamReaderService StreamReaderService { get; }

    public async IAsyncEnumerable<MississippiEvent> ReadStreamSliceAsync(
        long? fromVersion = null,
        long? toVersion = null
    )
    {
        string? compositeKey = this.GetPrimaryKeyString();
        string[] segments = compositeKey.Split('/');
        if (segments.Length < 3)
        {
            throw new InvalidOperationException($"Invalid slice grain key: '{compositeKey}'");
        }

        string streamId = segments[0];
        long sliceFrom = long.Parse(segments[1].Substring(1)); // 'f'<number>
        long sliceTo = long.Parse(segments[2].Substring(1)); // 't'<number>
        long start = fromVersion.HasValue ? Math.Max(sliceFrom, fromVersion.Value) : sliceFrom;
        long end = toVersion.HasValue ? Math.Min(sliceTo, toVersion.Value) : sliceTo;
        StreamGrainKey streamKey = new()
        {
            Id = streamId,
        };
        long expectedCount = (sliceTo - sliceFrom) + 1;
        if (Cache.Count < expectedCount)
        {
            long version = sliceFrom;
            await foreach (MississippiEvent ev in StreamReaderService.ReadStreamAsync(streamKey, sliceFrom, sliceTo))
            {
                Cache = Cache.SetItem(version, ev);
                version++;
            }
        }

        for (long v = start; v <= end; v++)
        {
            if (Cache.TryGetValue(v, out MississippiEvent? cachedEvent))
            {
                yield return cachedEvent;
            }
            else
            {
                throw new InvalidOperationException($"Event at version {v} not found in cache");
            }
        }
    }
}