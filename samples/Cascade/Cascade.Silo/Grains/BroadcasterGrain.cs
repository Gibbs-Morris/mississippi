using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Grains.Abstractions;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;

using Orleans;
using Orleans.Runtime;


namespace Cascade.Silo.Grains;

/// <summary>
///     Orleans grain that broadcasts messages to all SignalR clients via Aqueduct.
/// </summary>
/// <remarks>
///     <para>
///         This grain delegates to <see cref="ISignalRGroupGrain" /> to send messages
///         to all clients in the "broadcast" group. The group grain handles the
///         fan-out to individual client grains and stream publishing.
///     </para>
///     <para>
///         Clients are added to the "broadcast" group when they connect to the
///         MessageHub. This enables grain-to-client communication without the grain
///         needing direct access to streams or SignalR infrastructure.
///     </para>
/// </remarks>
[Alias("Cascade.Silo.BroadcasterGrain")]
internal sealed class BroadcasterGrain
    : IGrainBase,
      IBroadcasterGrain
{
    /// <summary>
    ///     Well-known group name for broadcast messages. Must match MessageHub.BroadcastGroupName.
    /// </summary>
    private const string BroadcastGroupName = "broadcast";

    /// <summary>
    ///     The hub name used for group grain key construction. Must match the SignalR hub type name.
    /// </summary>
    private const string HubName = "MessageHub";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BroadcasterGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="aqueductGrainFactory">The Aqueduct grain factory for managing groups.</param>
    /// <param name="logger">The logger instance.</param>
    public BroadcasterGrain(
        IGrainContext grainContext,
        IAqueductGrainFactory aqueductGrainFactory,
        ILogger<BroadcasterGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        AqueductGrainFactory = aqueductGrainFactory ?? throw new ArgumentNullException(nameof(aqueductGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IAqueductGrainFactory AqueductGrainFactory { get; }

    private ILogger<BroadcasterGrain> Logger { get; }

    /// <inheritdoc />
    public async Task BroadcastAsync(
        string message
    )
    {
        string grainKey = this.GetPrimaryKeyString();
        Logger.LogBroadcastingSent(grainKey, message);

        // Get the broadcast group grain and send the message
        // The group grain fans out to individual client grains, which publish to their server streams
        ISignalRGroupGrain groupGrain = AqueductGrainFactory.GetGroupGrain(HubName, BroadcastGroupName);
        await groupGrain.SendMessageAsync("ReceiveStreamMessage", [message, grainKey, DateTime.UtcNow]);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        string grainKey = this.GetPrimaryKeyString();
        Logger.LogBroadcasterActivated(grainKey);
        return Task.CompletedTask;
    }
}