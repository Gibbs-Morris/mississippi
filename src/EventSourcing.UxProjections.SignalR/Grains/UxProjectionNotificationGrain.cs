using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.AspNetCore.SignalR.Orleans.Grains;
using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     Orleans grain implementation that bridges projection cursor updates to SignalR clients.
/// </summary>
/// <remarks>
///     <para>
///         This grain subscribes to brook cursor update streams and pushes notifications
///         to SignalR groups. Each grain instance handles notifications for a specific
///         projection type and entity combination.
///     </para>
///     <para>
///         When a brook cursor moves, this grain calls <see cref="ISignalRGroupGrain" />
///         to notify all SignalR clients in the corresponding projection group. This ensures
///         proper separation between silo-hosted grains and ASP.NET-hosted SignalR connections,
///         with Orleans streams bridging the two deployments.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxProjectionNotificationGrain")]
internal sealed class UxProjectionNotificationGrain
    : IUxProjectionNotificationGrain,
      IAsyncObserver<BrookCursorMovedEvent>,
      IGrainBase
{
    private BrookPosition currentPosition = -1;

    private bool isSubscribed;

    private StreamSequenceToken? lastToken;

    private UxProjectionKey projectionKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionNotificationGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="brookStorageReader">Brook storage reader for fetching initial cursor position.</param>
    /// <param name="logger">Logger instance for logging notification grain operations.</param>
    public UxProjectionNotificationGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        IBrookStorageReader brookStorageReader,
        ILogger<UxProjectionNotificationGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        BrookStorageReader = brookStorageReader ?? throw new ArgumentNullException(nameof(brookStorageReader));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IBrookStorageReader BrookStorageReader { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<UxProjectionNotificationGrain> Logger { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <inheritdoc />
    public async Task ActivateAsync()
    {
        if (isSubscribed)
        {
            return;
        }

        string primaryKey = this.GetPrimaryKeyString();
        try
        {
            projectionKey = UxProjectionKey.FromString(primaryKey);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Logger.NotificationGrainInvalidPrimaryKey(primaryKey, ex);
            throw;
        }

        // Read initial cursor position from storage
        BrookKey brookKey = new(projectionKey.BrookKey.BrookName, projectionKey.BrookKey.EntityId);
        currentPosition = await BrookStorageReader.ReadCursorPositionAsync(brookKey, CancellationToken.None);

        // Subscribe to brook cursor updates
        StreamId streamId = StreamIdFactory.Create(brookKey);
        IAsyncStream<BrookCursorMovedEvent> stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookCursorMovedEvent>(streamId);
        await stream.SubscribeAsync(this);
        isSubscribed = true;
        Logger.NotificationGrainActivated(
            primaryKey,
            projectionKey.ProjectionTypeName,
            projectionKey.BrookKey.EntityId);
    }

    /// <inheritdoc />
    public Task<long> GetPositionAsync() => Task.FromResult(currentPosition.Value);

    /// <summary>
    ///     Handles stream completion.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnCompletedAsync()
    {
        Logger.NotificationStreamCompleted(projectionKey);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles errors on the subscribed stream.
    /// </summary>
    /// <param name="ex">The exception encountered on the stream.</param>
    /// <returns>A completed task.</returns>
    public Task OnErrorAsync(
        Exception ex
    )
    {
        Logger.NotificationStreamError(projectionKey, ex);
        this.DeactivateOnIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles a cursor moved event and notifies SignalR clients.
    /// </summary>
    /// <param name="item">The event containing the new brook cursor position.</param>
    /// <param name="token">Optional sequence token for ordering updates.</param>
    /// <returns>A task representing the asynchronous event handling operation.</returns>
    public async Task OnNextAsync(
        BrookCursorMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);

        // Skip if this is an older event
        if ((lastToken != null) && lastToken.Newer(token))
        {
            return;
        }

        lastToken = token;

        // Only notify if position actually moved forward
        if (!item.NewPosition.IsNewerThan(currentPosition))
        {
            return;
        }

        currentPosition = item.NewPosition;
        Logger.PositionUpdated(projectionKey, currentPosition);

        // Notify SignalR clients via the group grain
        // The group grain publishes to a stream that ASP.NET pods subscribe to
        string groupName = $"projection:{projectionKey.ProjectionTypeName}:{projectionKey.BrookKey.EntityId}";
        string hubGroupKey = $"{nameof(UxProjectionHub)}:{groupName}";
        ISignalRGroupGrain groupGrain = GrainFactory.GetGrain<ISignalRGroupGrain>(hubGroupKey);
        await groupGrain.SendMessageAsync(
            "OnProjectionChangedAsync",
            [projectionKey.ProjectionTypeName, projectionKey.BrookKey.EntityId, currentPosition.Value]);
        Logger.NotificationSent(projectionKey, currentPosition, groupName);
    }
}