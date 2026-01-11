using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Web.Contracts.Grains;
using Cascade.Web.Server.Hubs;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Cascade.Web.Server.Services;

/// <summary>
///     Background service that bridges Orleans streams to SignalR.
/// </summary>
/// <remarks>
///     This service subscribes to Orleans memory streams and forwards
///     messages to all connected SignalR clients. It demonstrates how
///     Orleans streaming can integrate with external messaging systems.
/// </remarks>
internal sealed class StreamBridgeService : BackgroundService
{
    private const string StreamProviderName = "StreamProvider";
    private const string StreamNamespace = "broadcast";
    private const string DefaultStreamKey = "default";

    private StreamSubscriptionHandle<StreamMessage>? Subscription { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamBridgeService" /> class.
    /// </summary>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="hubContext">The SignalR hub context for broadcasting to clients.</param>
    /// <param name="logger">The logger instance.</param>
    public StreamBridgeService(
        IClusterClient clusterClient,
        IHubContext<MessageHub> hubContext,
        ILogger<StreamBridgeService> logger
    )
    {
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        HubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IClusterClient ClusterClient { get; }

    private IHubContext<MessageHub> HubContext { get; }

    private ILogger<StreamBridgeService> Logger { get; }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogStreamBridgeStarting();

        try
        {
            // Get the stream provider and subscribe to the broadcast stream
            IStreamProvider streamProvider = ClusterClient.GetStreamProvider(StreamProviderName);
            IAsyncStream<StreamMessage> stream = streamProvider.GetStream<StreamMessage>(
                StreamId.Create(StreamNamespace, DefaultStreamKey));

            Subscription = await stream.SubscribeAsync(OnStreamMessageAsync);

            Logger.LogStreamBridgeSubscribed(StreamNamespace, DefaultStreamKey);

            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            Logger.LogStreamBridgeStopping();
        }
        catch (Exception ex)
        {
            Logger.LogStreamBridgeError(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Subscription is not null)
        {
            await Subscription.UnsubscribeAsync();
            Subscription = null;
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task OnStreamMessageAsync(StreamMessage message, StreamSequenceToken? token)
    {
        Logger.LogStreamMessageReceived(message.Sender, message.Content);

        // Forward the message to all SignalR clients
        await HubContext.Clients.All.SendAsync(
            "ReceiveStreamMessage",
            message.Content,
            message.Sender,
            message.Timestamp);
    }
}
