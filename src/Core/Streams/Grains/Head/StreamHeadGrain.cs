using Microsoft.Extensions.Logging;

using Mississippi.Core.Streams.StorageProvider;

using Orleans.Streams;


namespace Mississippi.Core.Streams.Grains.Head;

[ImplicitStreamSubscription(EventSourcingOrleansStreamNames.HeadUpdateStreamName)]
public class StreamHeadGrain
    : IStreamHeadGrain,
      IAsyncObserver<StreamHeadMovedEvent>,
      IGrainBase
{
    public StreamHeadGrain(
        IStreamStorageReader streamReaderProvider,
        IGrainContext grainContext,
        ILogger<StreamHeadGrain> logger
    )
    {
        StreamReaderProvider = streamReaderProvider;
        GrainContext = grainContext;
        Logger = logger;
    }

    private IAsyncStream<StreamHeadMovedEvent>? Stream { get; set; }

    private StreamSequenceToken? LastToken { get; set; }

    private ILogger<StreamHeadGrain> Logger { get; set; }

    private long Position { get; set; } = -1;

    private IStreamStorageReader StreamReaderProvider { get; }

    public async Task OnNextAsync(
        StreamHeadMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        if ((LastToken != null) && LastToken.Newer(token))
        {
            return;
        }

        LastToken = token;
        Position = item.NewPosition;
    }

    public async Task OnErrorAsync(
        Exception ex
    ) =>
        //Log error / should never happen.
        // then deactive grain so it kicks up agian next time.
        this.DeactivateOnIdle();

    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        Stream = this.GetStreamProvider("StreamProvider")
            .GetStream<StreamHeadMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, this.GetPrimaryKeyString()));
        await Stream.SubscribeAsync(this);
    }

    public IGrainContext GrainContext { get; }

    public async Task<long> GetLatestPositionAsync()
    {
        if (Position == -1)
        {
            Position = await StreamReaderProvider.ReadHeadPositionAsync(this.GetPrimaryKeyString());
        }

        return Position;
    }
}