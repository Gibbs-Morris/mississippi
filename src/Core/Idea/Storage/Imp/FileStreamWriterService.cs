using System.Collections.Immutable;
using System.Text.Json;

using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.Storage;

public class FileStreamWriterService : IStreamWriterService
{
    private const int ChunkSize = 100;

    private readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "streams");

    public FileStreamWriterService()
    {
        Directory.CreateDirectory(_basePath);
    }

    public async Task AppendEventAsync(
        StreamGrainKey streamId,
        MississippiEvent eventData,
        long expectedVersion = -1
    )
    {
        await AppendEventsInternal(streamId, new[] { eventData }, expectedVersion).ConfigureAwait(false);
    }

    public async Task AppendEventsAsync(
        StreamGrainKey streamId,
        ImmutableArray<MississippiEvent>[] events,
        long expectedVersion = -1
    )
    {
        MississippiEvent[] flat = events.SelectMany(arr => arr).ToArray();
        await AppendEventsInternal(streamId, flat, expectedVersion).ConfigureAwait(false);
    }

    private string GetHeadFilePath(
        StreamGrainKey streamId
    ) =>
        Path.Combine(_basePath, $"{streamId.ToOrleansKey()}.head");

    private string GetDataFilePath(
        StreamGrainKey streamId,
        long baseSeq
    ) =>
        Path.Combine(_basePath, $"{streamId.ToOrleansKey()}-{baseSeq}.json");

    private async Task AppendEventsInternal(
        StreamGrainKey streamId,
        MississippiEvent[] newEvents,
        long expectedVersion
    )
    {
        // Ensure base directory
        Directory.CreateDirectory(_basePath);
        // Read existing total count
        string headPath = GetHeadFilePath(streamId);
        long totalCount = 0;
        if (File.Exists(headPath))
        {
            string txt = await File.ReadAllTextAsync(headPath).ConfigureAwait(false);
            long.TryParse(txt, out totalCount);
        }

        if ((expectedVersion >= 0) && (totalCount != expectedVersion))
        {
            throw new InvalidOperationException(
                $"Concurrency conflict: expected version {expectedVersion}, actual {totalCount}");
        }

        int remaining = newEvents.Length;
        int processed = 0;
        while (remaining > 0)
        {
            long chunkIndex = totalCount / ChunkSize;
            int offset = (int)(totalCount % ChunkSize);
            int space = ChunkSize - offset;
            int take = Math.Min(space, remaining);
            // Load existing chunk
            string dataPath = GetDataFilePath(streamId, chunkIndex * ChunkSize);
            List<MississippiEvent> chunkList = new();
            if (File.Exists(dataPath))
            {
                using FileStream rdr = File.OpenRead(dataPath);
                chunkList = await JsonSerializer.DeserializeAsync<List<MississippiEvent>>(rdr).ConfigureAwait(false) ??
                            new List<MississippiEvent>();
            }

            // Append events for this chunk
            for (int i = 0; i < take; i++)
            {
                chunkList.Add(newEvents[processed + i]);
            }

            // Write chunk file
            using FileStream wr = File.Create(dataPath);
            await JsonSerializer.SerializeAsync(wr, chunkList).ConfigureAwait(false);
            // Advance counters
            processed += take;
            remaining -= take;
            totalCount += take;
        }

        // Update head file
        await File.WriteAllTextAsync(headPath, totalCount.ToString()).ConfigureAwait(false);
    }
}