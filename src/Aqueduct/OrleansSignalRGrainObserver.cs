using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct;

/// <summary>
///     Implementation of <see cref="ISignalRGrainObserver" /> that routes messages
///     through Orleans grains to SignalR clients.
/// </summary>
/// <remarks>
///     <para>
///         This implementation enables domain grains to send SignalR messages without
///         a direct dependency on SignalR infrastructure. Messages are routed through
///         the appropriate client or group grains based on the target.
///     </para>
///     <para>
///         Inject this into domain grains to enable real-time notifications to
///         connected SignalR clients.
///     </para>
/// </remarks>
public sealed class OrleansSignalRGrainObserver : ISignalRGrainObserver
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OrleansSignalRGrainObserver" /> class.
    /// </summary>
    /// <param name="clusterClient">The Orleans cluster client for grain operations.</param>
    /// <param name="options">Configuration options for the SignalR backplane.</param>
    /// <param name="logger">Logger instance for observer operations.</param>
    public OrleansSignalRGrainObserver(
        IClusterClient clusterClient,
        IOptions<OrleansSignalROptions> options,
        ILogger<OrleansSignalRGrainObserver> logger
    )
    {
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IClusterClient ClusterClient { get; }

    private ILogger<OrleansSignalRGrainObserver> Logger { get; }

    private IOptions<OrleansSignalROptions> Options { get; }

    /// <inheritdoc />
    public async Task SendToAllAsync(
        string hubName,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentException.ThrowIfNullOrEmpty(method);
        Logger.ObserverSendingToAll(hubName, method);
        string streamProviderName = Options.Value.StreamProviderName;
        IStreamProvider streamProvider = ClusterClient.GetStreamProvider(streamProviderName);
        StreamId streamId = StreamId.Create(Options.Value.AllClientsStreamNamespace, hubName);
        IAsyncStream<AllMessage> stream = streamProvider.GetStream<AllMessage>(streamId);
        AllMessage message = new()
        {
            MethodName = method,
            Args = [.. args],
        };
        await stream.OnNextAsync(message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendToConnectionAsync(
        string hubName,
        string connectionId,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(method);
        Logger.ObserverSendingToConnection(connectionId, hubName, method);
        ISignalRClientGrain clientGrain = GetClientGrain(hubName, connectionId);
        await clientGrain.SendMessageAsync(method, args).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendToGroupAsync(
        string hubName,
        string groupName,
        string method,
        ImmutableArray<object?> args,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentException.ThrowIfNullOrEmpty(groupName);
        ArgumentException.ThrowIfNullOrEmpty(method);
        Logger.ObserverSendingToGroup(groupName, hubName, method);
        ISignalRGroupGrain groupGrain = GetGroupGrain(hubName, groupName);
        await groupGrain.SendMessageAsync(method, args).ConfigureAwait(false);
    }

    private ISignalRClientGrain GetClientGrain(
        string hubName,
        string connectionId
    ) =>
        ClusterClient.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");

    private ISignalRGroupGrain GetGroupGrain(
        string hubName,
        string groupName
    ) =>
        ClusterClient.GetGrain<ISignalRGroupGrain>($"{hubName}:{groupName}");
}
