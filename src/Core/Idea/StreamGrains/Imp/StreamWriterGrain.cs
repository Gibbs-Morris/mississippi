using System.Collections.Immutable;

using Mississippi.Core.Idea.Storage;


namespace Mississippi.Core.Idea.StreamGrains.Imp;

public class StreamWriterGrain : IStreamWriterGrain
{
    public StreamWriterGrain(
        IStreamWriterService streamWriterService
    ) =>
        StreamWriterService = streamWriterService;

    private IStreamWriterService StreamWriterService { get; }

    public Task AppendEventAsync(
        MississippiEvent eventData,
        long expectedVersion = -1
    ) =>
        StreamWriterService.AppendEventAsync(
            new()
            {
                Id = this.GetPrimaryKeyString(),
            },
            eventData,
            expectedVersion);

    public Task AppendEventsAsync(
        ImmutableArray<MississippiEvent>[] events,
        long expectedVersion = -1
    ) =>
        StreamWriterService.AppendEventsAsync(
            new()
            {
                Id = this.GetPrimaryKeyString(),
            },
            events,
            expectedVersion);
}