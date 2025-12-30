using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Orleans grain implementation that tracks the brook cursor position for a specific UX projection type.
/// </summary>
/// <remarks>
///     <para>
///         This grain subscribes implicitly to brook cursor update events and maintains the latest
///         known position in memory. UX projection grains use this to determine if their cached
///         state is still current.
///     </para>
///     <para>
///         The grain is keyed by <see cref="UxProjectionKey" /> in the format
///         "projectionTypeName|brookType|brookId". On activation, it extracts the brook key
///         to subscribe to the correct cursor update stream.
///     </para>
/// </remarks>
internal sealed class UxProjectionCursorGrain
    : IUxProjectionCursorGrain,
      IAsyncObserver<BrookCursorMovedEvent>,
      IGrainBase
{
    private StreamSequenceToken? lastToken;

    private UxProjectionKey projectionKey;

    private BrookPosition trackedCursorPosition = -1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionCursorGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="brookStorageReader">Brook storage reader for fetching initial cursor position.</param>
    /// <param name="logger">Logger instance for logging cursor grain operations.</param>
    public UxProjectionCursorGrain(
        IGrainContext grainContext,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        IBrookStorageReader brookStorageReader,
        ILogger<UxProjectionCursorGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        BrookStorageReader = brookStorageReader ?? throw new ArgumentNullException(nameof(brookStorageReader));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IBrookStorageReader BrookStorageReader { get; }

    private ILogger<UxProjectionCursorGrain> Logger { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <inheritdoc />
    public Task DeactivateAsync()
    {
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<BrookPosition> GetPositionAsync() => Task.FromResult(trackedCursorPosition);

    /// <summary>
    ///     Subscribes the grain as an observer to the cursor update stream on activation.
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
            projectionKey = UxProjectionKey.FromString(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.CursorGrainInvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        // Read initial cursor position from storage before subscribing to the stream
        trackedCursorPosition = await BrookStorageReader.ReadCursorPositionAsync(projectionKey.BrookKey, token);

        // Subscribe to brook cursor updates using the brook key extracted from the projection key
        StreamId streamId = StreamIdFactory.Create(projectionKey.BrookKey);
        IAsyncStream<BrookCursorMovedEvent> stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookCursorMovedEvent>(streamId);
        await stream.SubscribeAsync(this);
        Logger.CursorGrainActivated(primaryKey, projectionKey.ProjectionTypeName, projectionKey.BrookKey);
    }

    /// <summary>
    ///     Handles stream completion.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnCompletedAsync()
    {
        Logger.StreamCompleted(projectionKey);
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
        Logger.StreamError(projectionKey, ex);
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles a cursor moved event and updates the grain's position if the event is newer.
    /// </summary>
    /// <param name="item">The event containing the new brook cursor position.</param>
    /// <param name="token">Optional sequence token for ordering updates.</param>
    /// <returns>A completed task representing the asynchronous event handling operation.</returns>
    public Task OnNextAsync(
        BrookCursorMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        if ((lastToken != null) && lastToken.Newer(token))
        {
            return Task.CompletedTask;
        }

        lastToken = token;
        if (item.NewPosition.IsNewerThan(trackedCursorPosition))
        {
            trackedCursorPosition = item.NewPosition;
            Logger.PositionUpdated(projectionKey, trackedCursorPosition);
        }

        return Task.CompletedTask;
    }
}