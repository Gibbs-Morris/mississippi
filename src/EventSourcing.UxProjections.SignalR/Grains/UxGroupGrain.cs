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
///     This grain is keyed by "{HubName}:{GroupName}" and persists the set of
///     connection identifiers belonging to the group.
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.UxGroupGrain")]
internal sealed class UxGroupGrain : IUxGroupGrain, IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxGroupGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="state">Persistent state for storing group membership.</param>
    /// <param name="logger">Logger instance for grain operations.</param>
    public UxGroupGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        [PersistentState("group", "UxSignalRStore")]
        IPersistentState<UxGroupState> state,
        ILogger<UxGroupGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<UxGroupGrain> Logger { get; }

    private IPersistentState<UxGroupState> State { get; }

    /// <inheritdoc />
    public async Task AddConnectionAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);

        string groupKey = this.GetPrimaryKeyString();
        Logger.AddingConnectionToGroup(connectionId, groupKey);

        if (State.State.ConnectionIds.Contains(connectionId))
        {
            Logger.ConnectionAlreadyInGroup(connectionId, groupKey);
            return;
        }

        State.State = State.State with
        {
            ConnectionIds = State.State.ConnectionIds.Add(connectionId),
        };

        await State.WriteStateAsync();

        Logger.ConnectionAddedToGroup(connectionId, groupKey, State.State.ConnectionIds.Count);
    }

    /// <inheritdoc />
    public Task<ImmutableHashSet<string>> GetConnectionsAsync()
    {
        return Task.FromResult(State.State.ConnectionIds);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        string groupKey = this.GetPrimaryKeyString();
        Logger.GroupGrainActivated(groupKey, State.State.ConnectionIds.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveConnectionAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);

        string groupKey = this.GetPrimaryKeyString();
        Logger.RemovingConnectionFromGroup(connectionId, groupKey);

        if (!State.State.ConnectionIds.Contains(connectionId))
        {
            Logger.ConnectionNotInGroup(connectionId, groupKey);
            return;
        }

        State.State = State.State with
        {
            ConnectionIds = State.State.ConnectionIds.Remove(connectionId),
        };

        await State.WriteStateAsync();

        Logger.ConnectionRemovedFromGroup(connectionId, groupKey, State.State.ConnectionIds.Count);

        // Deactivate if empty
        if (State.State.ConnectionIds.IsEmpty)
        {
            Logger.GroupNowEmpty(groupKey);
            this.DeactivateOnIdle();
        }
    }

    /// <inheritdoc />
    public async Task SendAllAsync(string methodName, object?[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        ArgumentNullException.ThrowIfNull(args);

        string groupKey = this.GetPrimaryKeyString();
        Logger.SendingToGroup(groupKey, methodName, State.State.ConnectionIds.Count);

        // Fan out to each connection
        foreach (string connectionId in State.State.ConnectionIds)
        {
            IUxClientGrain clientGrain = GrainFactory.GetGrain<IUxClientGrain>(connectionId);
            await clientGrain.SendMessageAsync(methodName, args);
        }

        Logger.SentToGroup(groupKey, methodName, State.State.ConnectionIds.Count);
    }
}
