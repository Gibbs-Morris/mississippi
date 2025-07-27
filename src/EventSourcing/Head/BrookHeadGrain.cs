using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Reader;

using Orleans.Streams;


namespace Mississippi.EventSourcing.Head;

/// <summary>
///     Orleans grain implementation that observes and maintains the head position of a Mississippi brook.
/// </summary>
[ImplicitStreamSubscription(EventSourcingOrleansStreamNames.HeadUpdateStreamName)]
internal class BrookHeadGrain
    : IBrookHeadGrain,
      IAsyncObserver<BrookHeadMovedEvent>,
      IGrainBase
{
    public BrookHeadGrain(
        IBrookStorageReader brookReaderProvider,
        IGrainContext grainContext,
        ILogger<BrookHeadGrain> logger,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory
    )
    {
        BrookReaderProvider = brookReaderProvider;
        GrainContext = grainContext;
        Logger = logger;
        StreamProviderOptions = streamProviderOptions;
        StreamIdFactory = streamIdFactory;
    }

    private IAsyncStream<BrookHeadMovedEvent>? Stream { get; set; }

    private StreamSequenceToken? LastToken { get; set; }

    private ILogger<BrookHeadGrain> Logger { get; set; }

    private BrookPosition TrackedHeadPosition { get; set; } = -1;

    private IBrookStorageReader BrookReaderProvider { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    /// <summary>
    ///     Handles a head moved event and updates the grain's position if the event is newer.
    /// </summary>
    /// <param name="item">The event containing the new brook head position.</param>
    /// <param name="token">Optional sequence token for ordering updates.</param>
    public Task OnNextAsync(
        BrookHeadMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        if ((LastToken != null) && LastToken.Newer(token))
        {
            return Task.CompletedTask;
        }

        LastToken = token;
        if (item.NewPosition.IsNewerThan(TrackedHeadPosition))
        {
            TrackedHeadPosition = item.NewPosition;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles errors on the subscribed stream and deactivates the grain.
    /// </summary>
    /// <param name="ex">The exception encountered on the stream.</param>
    public Task OnErrorAsync(
        Exception ex
    )
    {
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Gets the latest position of the brook head, loading from storage if not initialized.
    /// </summary>
    /// <returns>The most recent persisted head position.</returns>
    public async Task<BrookPosition> GetLatestPositionAsync()
    {
        if (TrackedHeadPosition.NotSet)
        {
            BrookPosition trackedPosition = await BrookReaderProvider.ReadHeadPositionAsync(this.GetPrimaryKeyString());
            if (trackedPosition.IsNewerThan(TrackedHeadPosition))
            {
                TrackedHeadPosition = trackedPosition;
            }
        }

        return TrackedHeadPosition;
    }

    /// <summary>
    ///     Subscribes the grain as an observer to the head update stream on activation.
    /// </summary>
    /// <param name="token">Cancellation token for activation.</param>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        StreamId key = StreamIdFactory.Create(this.GetPrimaryKeyString());
        Stream = this.GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookHeadMovedEvent>(key);
        await Stream.SubscribeAsync(this);
    }

    public IGrainContext GrainContext { get; }
}