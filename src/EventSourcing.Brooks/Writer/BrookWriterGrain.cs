using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Abstractions.Writer;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.EventSourcing.Brooks.Writer;

/// <summary>
///     Orleans grain implementation for writing events to a Mississippi brook (event stream).
///     Handles event appending and manages the writer lifecycle for a specific brook.
/// </summary>
internal sealed class BrookWriterGrain
    : IBrookWriterGrain,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookWriterGrain" /> class.
    ///     Sets up the grain with required dependencies for brook writing operations.
    /// </summary>
    /// <param name="brookWriterService">The brook storage writer service for persisting events.</param>
    /// <param name="logger">Logger instance for logging writer operations.</param>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
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

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    private IBrookStorageWriter BrookWriterService { get; }

    private ILogger<BrookWriterGrain> Logger { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <summary>
    ///     Appends a collection of events to the brook (event stream).
    ///     Validates the expected cursor position and publishes cursor movement events after successful append.
    /// </summary>
    /// <param name="events">The immutable array of events to append to the brook.</param>
    /// <param name="expectedCursorPosition">Optional expected current cursor position for optimistic concurrency control.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The new cursor position of the brook after appending the events.</returns>
    public async Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedCursorPosition = null,
        CancellationToken cancellationToken = default
    )
    {
        BrookKey key = BrookKey.FromString(this.GetPrimaryKeyString());
        Logger.AppendingEvents(key, events.Length, expectedCursorPosition?.Value);
        Stopwatch sw = Stopwatch.StartNew();
        BrookPosition newPosition = await BrookWriterService.AppendEventsAsync(
            key,
            events,
            expectedCursorPosition,
            cancellationToken);
        sw.Stop();
        Logger.EventsAppended(key, events.Length, newPosition.Value, sw.ElapsedMilliseconds);
        Logger.PublishingCursorMoved(key, newPosition.Value);
        IAsyncStream<BrookCursorMovedEvent> stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookCursorMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.CursorUpdateStreamName, this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new(this.GetPrimaryKeyString(), newPosition));
        Logger.CursorMovedEventPublished(key, newPosition.Value);
        return newPosition;
    }

    /// <summary>
    ///     Called when the grain is activated.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the activation operation.</returns>
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        BrookKey key = BrookKey.FromString(this.GetPrimaryKeyString());
        Logger.Activated(key);
        return Task.CompletedTask;
    }
}