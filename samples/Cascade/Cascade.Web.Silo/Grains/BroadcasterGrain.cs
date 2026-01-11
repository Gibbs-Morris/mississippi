using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Web.Contracts.Grains;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Cascade.Web.Silo.Grains;

/// <summary>
///     Orleans grain that broadcasts messages to all SignalR clients via Aqueduct.
/// </summary>
/// <remarks>
///     This grain demonstrates Orleans streaming integrated with Aqueduct.
///     Messages are published to the Aqueduct all-clients stream and delivered
///     to SignalR clients via <c>OrleansHubLifetimeManager</c>.
/// </remarks>
[Alias("Cascade.Web.Silo.BroadcasterGrain")]
internal sealed class BroadcasterGrain : IGrainBase, IBroadcasterGrain
{
    /// <summary>
    ///     The hub name used for stream routing. Must match the SignalR hub type name.
    /// </summary>
    private const string HubName = "MessageHub";

    private IAsyncStream<AllMessage>? Stream { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BroadcasterGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="options">The Aqueduct configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public BroadcasterGrain(
        IGrainContext grainContext,
        IOptions<OrleansSignalROptions> options,
        ILogger<BroadcasterGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<BroadcasterGrain> Logger { get; }

    private IOptions<OrleansSignalROptions> Options { get; }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        // Get the Aqueduct stream provider and create a stream for all-clients broadcast
        IStreamProvider streamProvider = this.GetStreamProvider(Options.Value.StreamProviderName);
        string grainKey = this.GetPrimaryKeyString();

        // Create a stream using Aqueduct's all-clients namespace with the hub name as key
        Stream = streamProvider.GetStream<AllMessage>(
            StreamId.Create(Options.Value.AllClientsStreamNamespace, HubName));

        Logger.LogBroadcasterActivated(grainKey);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(string message)
    {
        if (Stream is null)
        {
            throw new InvalidOperationException("Stream not initialized. Grain may not be activated.");
        }

        string grainKey = this.GetPrimaryKeyString();

        // Create an Aqueduct AllMessage for broadcasting to all SignalR clients
        AllMessage streamMessage = new()
        {
            MethodName = "ReceiveStreamMessage",
            Args = [message, grainKey, DateTime.UtcNow],
        };

        Logger.LogBroadcastingSent(grainKey, message);

        await Stream.OnNextAsync(streamMessage);
    }
}
