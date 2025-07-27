using System.Collections.Immutable;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Brooks;
using Mississippi.EventSourcing.Abstractions.Providers.Storage;
using Mississippi.EventSourcing.Brooks.Grains.Head;
using Mississippi.EventSourcing.Brooks.Grains.Reader;

using Orleans.Streams;


namespace Mississippi.EventSourcing.Brooks.Grains.Writer;

internal class BrookWriterGrain
    : IGrainBase,
      IBrookWriterGrain
{
    public BrookWriterGrain(
        IBrookStorageWriter brookWriterService,
        ILogger<BrookWriterGrain> logger,
        IGrainContext grainContext,
        IOptions<BrookProviderOptions> streamProviderOptions
    )
    {
        BrookWriterService = brookWriterService;
        Logger = logger;
        GrainContext = grainContext;
        StreamProviderOptions = streamProviderOptions;
    }

    private IBrookStorageWriter BrookWriterService { get; }

    private ILogger<BrookWriterGrain> Logger { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    public async Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    )
    {
        BrookKey key = this.GetPrimaryKeyString();
        BrookPosition newPosition = await BrookWriterService.AppendEventsAsync(
            key,
            events,
            expectedHeadPosition,
            cancellationToken);
        IAsyncStream<BrookHeadMovedEvent>? stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookHeadMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new(newPosition));
        return newPosition;
    }

    public IGrainContext GrainContext { get; }
}