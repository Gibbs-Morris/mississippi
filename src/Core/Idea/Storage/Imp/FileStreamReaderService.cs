using System.Text.Json;

using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.Storage;

public class FileStreamReaderService : IStreamReaderService
{
    private const int ChunkSize = 100;

    private readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "streams");

    public FileStreamReaderService()
    {
        Directory.CreateDirectory(_basePath);
    }

    public async IAsyncEnumerable<MississippiEvent> ReadStreamAsync(
        StreamGrainKey streamId,
        long? fromVersion = null,
        long? toVersion = null
    )
    {
        string headPath = GetHeadFilePath(streamId);
        if (!File.Exists(headPath))
        {
            yield break;
        }

        string headJson = await File.ReadAllTextAsync(headPath).ConfigureAwait(false);
        long totalCount;
        try
        {
            totalCount = JsonSerializer.Deserialize<long>(headJson);
        }
        catch
        {
            yield break;
        }

        if (totalCount == 0)
        {
            yield break;
        }

        long skip = fromVersion.GetValueOrDefault(0);
        long end = toVersion.GetValueOrDefault(totalCount);
        for (long baseSeq = 0; baseSeq < totalCount; baseSeq += ChunkSize)
        {
            string dataPath = GetDataFilePath(streamId, baseSeq);
            if (!File.Exists(dataPath))
            {
                continue;
            }

            List<MississippiEvent> events;
            using (FileStream s = File.OpenRead(dataPath))
            {
                events = await JsonSerializer.DeserializeAsync<List<MississippiEvent>>(s).ConfigureAwait(false) ??
                         new List<MississippiEvent>();
            }

            for (int i = 0; i < events.Count; i++)
            {
                long version = baseSeq + i + 1;
                if (version <= skip)
                {
                    continue;
                }

                if (version > end)
                {
                    yield break;
                }

                yield return events[i];
            }
        }
    }

    public async Task<long> GetLatestStreamVersionAsync(
        StreamGrainKey streamId
    )
    {
        string headPath = GetHeadFilePath(streamId);
        if (!File.Exists(headPath))
        {
            return 0;
        }

        string headJson2 = await File.ReadAllTextAsync(headPath).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<long>(headJson2);
        }
        catch
        {
            return 0;
        }
    }

    private string GetHeadFilePath(
        StreamGrainKey streamId
    )
    {
        return Path.Combine(_basePath, $"{streamId.ToOrleansKey()}.head");
    }

    private string GetDataFilePath(
        StreamGrainKey streamId,
        long baseSeq
    )
    {
        return Path.Combine(_basePath, $"{streamId.ToOrleansKey()}-{baseSeq}.json");
    }
}