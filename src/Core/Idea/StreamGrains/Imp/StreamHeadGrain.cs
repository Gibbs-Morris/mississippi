using Mississippi.Core.Idea.Storage;


namespace Mississippi.Core.Idea.StreamGrains.Imp;

public class StreamHeadGrain : IStreamHeadGrain
{
    public StreamHeadGrain(
        IStreamReaderService streamReaderService
    )
    {
        StreamReaderService = streamReaderService;
    }

    private IStreamReaderService StreamReaderService { get; }

    public Task<long> GetLatestVersionAsync()
    {
        return StreamReaderService.GetLatestStreamVersionAsync(
            new()
            {
                Id = this.GetPrimaryKeyString(),
            });
    }
}