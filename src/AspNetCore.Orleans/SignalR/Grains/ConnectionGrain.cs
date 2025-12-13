namespace Mississippi.AspNetCore.Orleans.SignalR.Grains;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using global::Orleans;
using global::Orleans.Runtime;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orleans grain implementation for SignalR connection state management.
/// </summary>
internal sealed class ConnectionGrain : IGrainBase, IConnectionGrain
{
    /// <summary>
    /// Gets the grain context required by IGrainBase.
    /// </summary>
    public IGrainContext GrainContext { get; }

    private ILogger<ConnectionGrain> Logger { get; }
    private IPersistentState<ConnectionState> State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionGrain"/> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="state">The persistent state.</param>
    public ConnectionGrain(
        IGrainContext grainContext,
        ILogger<ConnectionGrain> logger,
        [PersistentState("connection", "SignalRStorage")]
        IPersistentState<ConnectionState> state)
    {
        GrainContext = grainContext;
        Logger = logger;
        State = state;
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(string? userId, string[] groups)
    {
        string connectionId = this.GetPrimaryKeyString();
        State.State.Data = new ConnectionData
        {
            ConnectionId = connectionId,
            UserId = userId,
            Groups = groups ?? [],
        };
        await State.WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<ConnectionData?> GetAsync()
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return Task.FromResult<ConnectionData?>(null);
        }

        return Task.FromResult<ConnectionData?>(State.State.Data);
    }

    /// <inheritdoc/>
    public async Task AddToGroupAsync(string groupName)
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return;
        }

        List<string> groups = [.. State.State.Data.Groups];
        if (!groups.Contains(groupName))
        {
            groups.Add(groupName);
            State.State.Data = State.State.Data with { Groups = [.. groups] };
            await State.WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveFromGroupAsync(string groupName)
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return;
        }

        List<string> groups = [.. State.State.Data.Groups];
        if (groups.Remove(groupName))
        {
            State.State.Data = State.State.Data with { Groups = [.. groups] };
            await State.WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public async Task UnregisterAsync()
    {
        await State.ClearStateAsync();
    }

    [GenerateSerializer]
    [Alias("Mississippi.AspNetCore.Orleans.SignalR.ConnectionState")]
    internal sealed record ConnectionState
    {
        [Id(0)]
        public ConnectionData? Data { get; set; }
    }
}
