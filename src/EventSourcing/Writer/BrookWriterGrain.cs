using System.Collections.Immutable;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;

using Orleans.Streams;


namespace Mississippi.EventSourcing.Writer;

/// <summary>
///     Orleans grain implementation for writing events to a Mississippi brook (event stream).
///     Handles event appending and manages the writer lifecycle for a specific brook.
/// </summary>
internal class BrookWriterGrain
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

    private IBrookStorageWriter BrookWriterService { get; }

    private ILogger<BrookWriterGrain> Logger { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    ///     Provides access to Orleans infrastructure services and grain lifecycle management.
    /// </summary>
    /// <value>The grain context instance.</value>
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Appends a collection of events to the brook (event stream).
    ///     Validates the expected head position and publishes head movement events after successful append.
    /// </summary>
    /// <param name="events">The immutable array of events to append to the brook.</param>
    /// <param name="expectedHeadPosition">Optional expected current head position for optimistic concurrency control.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    /// <returns>The new head position of the brook after appending the events.</returns>
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
        IAsyncStream<BrookHeadMovedEvent> stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookHeadMovedEvent>(
                StreamId.Create(EventSourcingOrleansStreamNames.HeadUpdateStreamName, this.GetPrimaryKeyString()));
        await stream.OnNextAsync(new(newPosition));
        return newPosition;
    }
}