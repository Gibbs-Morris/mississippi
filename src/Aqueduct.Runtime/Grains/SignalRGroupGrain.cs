using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Diagnostics;
using Mississippi.Aqueduct.Runtime.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.Grains;

/// <summary>
///     Orleans grain implementation that tracks group membership for a SignalR group.
/// </summary>
/// <remarks>
///     <para>
///         This grain is keyed by "{HubName}:{GroupName}" and maintains the set of
///         connection identifiers belonging to the group.
///     </para>
///     <para>
///         State is maintained in-memory. When all connections leave a group, the grain
///         deactivates. Group membership is rebuilt as connections join.
///     </para>
///     <para>
///         <b>Deployment Note:</b> This grain runs on Orleans silo hosts. Other grains
///         (e.g., UxProjectionNotificationGrain) call <see cref="SendMessageAsync" /> to
///         broadcast messages to group members. The grain fans out to individual
///         <see cref="ISignalRClientGrain" /> instances.
///     </para>
/// </remarks>
[Alias("Mississippi.Aqueduct.Runtime.Grains.SignalRGroupGrain")]
internal sealed class SignalRGroupGrain
    : ISignalRGroupGrain,
      IGrainBase,
      IDisposable
{
    private IGrainTimer? cleanupTimer;

    private SignalRGroupState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRGroupGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainRuntime">The Orleans runtime for activation lifecycle control.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="options">Configuration options for failure cleanup timing.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public SignalRGroupGrain(
        IGrainContext grainContext,
        IGrainRuntime grainRuntime,
        IGrainFactory grainFactory,
        IOptions<AqueductOptions> options,
        ILogger<SignalRGroupGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainRuntime = grainRuntime ?? throw new ArgumentNullException(nameof(grainRuntime));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IGrainFactory GrainFactory { get; }

    private IGrainRuntime GrainRuntime { get; }

    private ILogger<SignalRGroupGrain> Logger { get; }

    private IOptions<AqueductOptions> Options { get; }

    private static string ExtractHubName(
        string groupKey
    )
    {
        // Group key format: "HubName:GroupName"
        int separatorIndex = groupKey.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? groupKey[..separatorIndex] : groupKey;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Membership changes run synchronously and may interleave with a broadcast awaiting a client.
    ///     This avoids a client/group call cycle during joins or disconnect cleanup.
    /// </remarks>
    public Task AddConnectionAsync(
        string connectionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        string groupKey = this.GetPrimaryKeyString();
        Logger.AddingConnectionToGroup(connectionId, groupKey);
        if (state.ConnectionIds.Contains(connectionId))
        {
            Logger.ConnectionAlreadyInGroup(connectionId, groupKey);
            return Task.CompletedTask;
        }

        EnsureCleanupTimer();
        GrainRuntime.DelayDeactivation(GrainContext, Timeout.InfiniteTimeSpan);
        state = state with
        {
            ConnectionIds = state.ConnectionIds.Add(connectionId),
        };
        string hubName = ExtractHubName(groupKey);
        AqueductMetrics.RecordGroupJoin(hubName);
        Logger.ConnectionAddedToGroup(connectionId, groupKey, state.ConnectionIds.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        cleanupTimer?.Dispose();
        cleanupTimer = null;
    }

    /// <inheritdoc />
    public Task<ImmutableHashSet<string>> GetConnectionsAsync() => Task.FromResult(state.ConnectionIds);

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        string groupKey = this.GetPrimaryKeyString();
        Logger.GroupGrainActivated(groupKey, state.ConnectionIds.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Membership changes may interleave with broadcasts, which enumerate an immutable membership snapshot.
    /// </remarks>
    public Task RemoveConnectionAsync(
        string connectionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        string groupKey = this.GetPrimaryKeyString();
        Logger.RemovingConnectionFromGroup(connectionId, groupKey);
        if (!state.ConnectionIds.Contains(connectionId))
        {
            Logger.ConnectionNotInGroup(connectionId, groupKey);
            return Task.CompletedTask;
        }

        state = state with
        {
            ConnectionIds = state.ConnectionIds.Remove(connectionId),
        };
        string hubName = ExtractHubName(groupKey);
        AqueductMetrics.RecordGroupLeave(hubName);
        Logger.ConnectionRemovedFromGroup(connectionId, groupKey, state.ConnectionIds.Count);

        // Deactivate if empty
        if (state.ConnectionIds.IsEmpty)
        {
            Dispose();
            Logger.GroupNowEmpty(groupKey);
            this.DeactivateOnIdle();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(
        string method,
        ImmutableArray<object?> args
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        string groupKey = this.GetPrimaryKeyString();
        string hubName = ExtractHubName(groupKey);
        ImmutableHashSet<string> connections = state.ConnectionIds;
        int connectionCount = connections.Count;
        Logger.SendingToGroup(groupKey, method, connectionCount);
        IEnumerable<Task> sends = connections.Select(connectionId =>
            SendMessageToConnectionAsync(hubName, connectionId, method, args));
        await Task.WhenAll(sends).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        AqueductMetrics.RecordGroupMessageSent(hubName, method, connectionCount);
        Logger.SentToGroup(groupKey, method, connectionCount);
    }

    private void EnsureCleanupTimer()
    {
        TimeSpan interval = TimeSpan.FromMinutes(Options.Value.HeartbeatIntervalMinutes);
        cleanupTimer ??= GrainRuntime.TimerRegistry.RegisterGrainTimer(
            GrainContext,
            static (grain, _) => grain.RemoveOrphanedConnectionsAsync(),
            this,
            new()
            {
                DueTime = interval,
                Period = interval,
                Interleave = false,
            });
    }

    /// <summary>
    ///     Removes a disconnected member while preserving failed lookups for the next sweep.
    /// </summary>
    /// <param name="hubName">The hub owning the group.</param>
    /// <param name="connectionId">The member in the current sweep snapshot.</param>
    /// <returns>The member's cleanup operation.</returns>
    private async Task RemoveDisconnectedConnectionAsync(
        string hubName,
        string connectionId
    )
    {
        try
        {
            ISignalRClientGrain client = GrainFactory.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");
            if (await client.GetServerIdAsync().ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext) is null)
            {
                await RemoveConnectionAsync(connectionId)
                    .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            }
        }
        catch (Exception ex) when (ex is OrleansException or TimeoutException)
        {
            Logger.ConnectionLivenessCheckFailed(this.GetPrimaryKeyString(), connectionId, ex);
        }
    }

    private async Task RemoveOrphanedConnectionsAsync()
    {
        string hubName = ExtractHubName(this.GetPrimaryKeyString());
        await Task.WhenAll(
                state.ConnectionIds.Select(connectionId => RemoveDisconnectedConnectionAsync(hubName, connectionId)))
            .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
    }

    /// <summary>
    ///     Isolates client lookup and invocation failures in the recipient's asynchronous send task.
    /// </summary>
    /// <param name="hubName">The hub owning the group.</param>
    /// <param name="connectionId">The snapshot member receiving the message.</param>
    /// <param name="method">The SignalR method name.</param>
    /// <param name="args">The message arguments.</param>
    /// <returns>The recipient's send operation.</returns>
    private async Task SendMessageToConnectionAsync(
        string hubName,
        string connectionId,
        string method,
        ImmutableArray<object?> args
    )
    {
        ISignalRClientGrain clientGrain = GrainFactory.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");
        await clientGrain.SendMessageAsync(method, args)
            .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
    }
}
