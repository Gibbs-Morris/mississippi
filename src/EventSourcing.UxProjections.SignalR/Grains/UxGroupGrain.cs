using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

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
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxGroupGrain")]
internal sealed class UxGroupGrain : IUxGroupGrain, IGrainBase
{
    private UxGroupState state = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxGroupGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxGroupGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        ILogger<UxGroupGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<UxGroupGrain> Logger { get; }

    /// <inheritdoc />
    public Task AddConnectionAsync(string connectionId)
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
    public Task<ImmutableHashSet<string>> GetConnectionsAsync()
    {
        return Task.FromResult(state.ConnectionIds);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        string groupKey = this.GetPrimaryKeyString();
        Logger.GroupGrainActivated(groupKey, state.ConnectionIds.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveConnectionAsync(string connectionId)
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
    public async Task SendAllAsync(string methodName, object?[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        ArgumentNullException.ThrowIfNull(args);

        string groupKey = this.GetPrimaryKeyString();
        Logger.SendingToGroup(groupKey, methodName, state.ConnectionIds.Count);

        // Fan out to each connection
        foreach (string connectionId in state.ConnectionIds)
        {
            IUxClientGrain clientGrain = GrainFactory.GetGrain<IUxClientGrain>(connectionId);
            await clientGrain.SendMessageAsync(methodName, args);
        }

        Logger.SentToGroup(groupKey, methodName, state.ConnectionIds.Count);
    }
}
