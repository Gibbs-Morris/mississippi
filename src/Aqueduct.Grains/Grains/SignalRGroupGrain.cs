using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Grains.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Grains.Grains;

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
[Alias("Mississippi.Aqueduct.SignalRGroupGrain")]
internal sealed class SignalRGroupGrain
    : ISignalRGroupGrain,
      IGrainBase
{
    private SignalRGroupState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRGroupGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public SignalRGroupGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        ILogger<SignalRGroupGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<SignalRGroupGrain> Logger { get; }

    /// <inheritdoc />
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

        state = state with
        {
            ConnectionIds = state.ConnectionIds.Add(connectionId),
        };
        Logger.ConnectionAddedToGroup(connectionId, groupKey, state.ConnectionIds.Count);
        return Task.CompletedTask;
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
        Logger.ConnectionRemovedFromGroup(connectionId, groupKey, state.ConnectionIds.Count);

        // Deactivate if empty
        if (state.ConnectionIds.IsEmpty)
        {
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
        Logger.SendingToGroup(groupKey, method, state.ConnectionIds.Count);

        // Fan out to each connection
        foreach (string connectionId in state.ConnectionIds)
        {
            ISignalRClientGrain clientGrain = GrainFactory.GetGrain<ISignalRClientGrain>($"{hubName}:{connectionId}");
            await clientGrain.SendMessageAsync(method, args).ConfigureAwait(false);
        }

        Logger.SentToGroup(groupKey, method, state.ConnectionIds.Count);
    }

    private static string ExtractHubName(
        string groupKey
    )
    {
        // Group key format: "HubName:GroupName"
        int separatorIndex = groupKey.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? groupKey[..separatorIndex] : groupKey;
    }
}