using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Providers.Storage;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.Core.Brooks.Grains.Head;
using Mississippi.Core.Brooks.Grains.Reader;

namespace Mississippi.Core.Brooks.Grains.Writer;

internal class BrookWriterGrain
    : IGrainBase,
        IBrookWriterGrain
{
    public BrookWriterGrain(
        IBrookStorageWriter brookWriterService,
        ILogger<BrookWriterGrain> logger,
        IGrainContext grainContext, 
        IOptions<BrookProviderOptions> streamProviderOptions)
    {
        BrookWriterService = brookWriterService;
        Logger = logger;
        GrainContext = grainContext;
        StreamProviderOptions = streamProviderOptions;
    }

    private IBrookStorageWriter BrookWriterService { get; }

    private ILogger<BrookWriterGrain> Logger { get; }

    public IGrainContext GrainContext { get; }

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
        var stream = this.GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<StreamHeadMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new StreamHeadMovedEvent(newPosition));
        return newPosition;
    }
}