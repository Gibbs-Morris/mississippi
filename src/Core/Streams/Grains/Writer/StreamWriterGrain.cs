using System.Collections.Immutable;

using Microsoft.Extensions.Logging;

using Mississippi.Core.Streams.Grains.Head;
using Mississippi.Core.Streams.Grains.Reader;
using Mississippi.Core.Streams.StorageProvider;

using Orleans.Streams;

using StreamPosition = Mississippi.Core.Streams.Grains.StreamPosition;


namespace Mississippi.Core.Streams.Grains.Writer;

public class StreamWriterGrain
    : IGrainBase,
      IStreamWriterGrain
{
    public StreamWriterGrain(
        IStreamStorageWriter streamWriterService,
        ILogger<StreamWriterGrain> logger,
        IGrainContext grainContext
    )
    {
        StreamWriterService = streamWriterService;
        Logger = logger;
        GrainContext = grainContext;
    }

    private IStreamStorageWriter StreamWriterService { get; }

    private ILogger<StreamWriterGrain> Logger { get; }

    public IGrainContext GrainContext { get; }

    public async Task<StreamPosition> AppendEventsAsync(
        ImmutableArray<MississippiEvent> events,
        StreamPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    )
    {
        StreamCompositeKey key = this.GetPrimaryKeyString();
        StreamPosition newPosition = await StreamWriterService.AppendEventsAsync(
            key,
            events,
            expectedHeadPosition,
            cancellationToken);
        IAsyncStream<StreamHeadMovedEvent>? stream = this.GetStreamProvider("StreamProvider")
            .GetStream<StreamHeadMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new(newPosition));
        return newPosition;
    }
}