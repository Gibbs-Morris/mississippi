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
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookHeadGrain" /> class.
    ///     Sets up the grain with required dependencies for brook head position tracking.
    /// </summary>
    /// <param name="brookReaderProvider">The brook storage reader service for reading head positions.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="logger">Logger instance for logging head grain operations.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
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

    private ILogger<BrookHeadGrain> Logger { get; }

    private BrookPosition TrackedHeadPosition { get; set; } = -1;

    private BrookKey BrookId { get; set; }

    private IBrookStorageReader BrookReaderProvider { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Handles a head moved event and updates the grain's position if the event is newer.
    /// </summary>
    /// <param name="item">The event containing the new brook head position.</param>
    /// <param name="token">Optional sequence token for ordering updates.</param>
    /// <returns>A completed task representing the asynchronous event handling operation.</returns>
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
    /// <returns>A completed task representing the asynchronous error handling operation.</returns>
    public Task OnErrorAsync(
        Exception ex
    )
    {
        // Deactivate to force a clean resubscribe on next activation
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Gets the latest position of the brook head, loading from storage if not initialized.
    /// </summary>
    /// <returns>The most recent persisted head position.</returns>
    public Task<BrookPosition> GetLatestPositionAsync() =>

        // Fast path: return cached head. Writers publish updates on success.
        Task.FromResult(TrackedHeadPosition);

    /// <summary>
    ///     Gets the latest position by issuing a strongly consistent storage read and updating the cache if newer.
    /// </summary>
    /// <returns>The confirmed most recent persisted head position.</returns>
    public async Task<BrookPosition> GetLatestPositionConfirmedAsync()
    {
        // Strongly consistent path: read from storage and update cache if newer.
        BrookPosition storagePosition = await BrookReaderProvider.ReadHeadPositionAsync(BrookId);
        if (storagePosition.IsNewerThan(TrackedHeadPosition))
        {
            TrackedHeadPosition = storagePosition;
        }

        return TrackedHeadPosition;
    }

    /// <summary>
    ///     Deactivate the grain on idle, used by tests to flush caches and lifecycle state.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    public Task DeactivateAsync()
    {
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Subscribes the grain as an observer to the head update stream on activation.
    /// </summary>
    /// <param name="token">Cancellation token for activation.</param>
    /// <returns>A task representing the asynchronous activation operation.</returns>
    public async Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        try
        {
            BrookId = BrookKey.FromString(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.InvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        StreamId key = StreamIdFactory.Create(BrookId);
        Stream = this.GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookHeadMovedEvent>(key);
        await Stream.SubscribeAsync(this);
    }
}