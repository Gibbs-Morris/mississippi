using System.Collections.Immutable;

using Microsoft.Extensions.Logging;

using Mississippi.Core.Idea.Storage;


namespace Mississippi.Core.Idea.StreamGrains.Imp;

public class StreamWriterGrain : IStreamWriterGrain
{
    public StreamWriterGrain(
        IStreamWriterService streamWriterService,
        ILogger<StreamWriterGrain> logger
    )
    {
        StreamWriterService = streamWriterService;
        Logger = logger;
    }

    private IStreamWriterService StreamWriterService { get; }

    private ILogger<StreamWriterGrain> Logger { get; }

    public Task AppendEventAsync(
        MississippiEvent eventData,
        long expectedVersion = -1
    )
    {
        return StreamWriterService.AppendEventAsync(
            new()
            {
                Id = this.GetPrimaryKeyString(),
            },
            eventData,
            expectedVersion);
    }

    public Task AppendEventsAsync(
        ImmutableArray<MississippiEvent>[] events,
        long expectedVersion = -1
    )
    {
        return StreamWriterService.AppendEventsAsync(
            new()
            {
                Id = this.GetPrimaryKeyString(),
            },
            events,
            expectedVersion);
    }
}