using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.AspNetCore.Orleans.Authentication.Grains;

/// <summary>
///     Orleans grain implementation for authentication ticket storage.
/// </summary>
internal sealed class AuthTicketGrain
    : IGrainBase,
      IAuthTicketGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthTicketGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="state">The persistent state.</param>
    public AuthTicketGrain(
        IGrainContext grainContext,
        ILogger<AuthTicketGrain> logger,
        TimeProvider timeProvider,
        [PersistentState("authTicket", "TicketStorage")]
        IPersistentState<AuthTicketState> state
    )
    {
        GrainContext = grainContext;
        Logger = logger;
        TimeProvider = timeProvider;
        State = state;
    }

    /// <summary>
    ///     Gets the grain context required by IGrainBase.
    /// </summary>
    public IGrainContext GrainContext { get; }

    private ILogger<AuthTicketGrain> Logger { get; }

    private IPersistentState<AuthTicketState> State { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public Task<AuthTicketData?> GetAsync()
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return Task.FromResult<AuthTicketData?>(null);
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        AuthTicketData data = State.State.Data;
        if (data.ExpiresAt <= now)
        {
            return Task.FromResult<AuthTicketData?>(null);
        }

        return Task.FromResult<AuthTicketData?>(data);
    }

    /// <inheritdoc />
    public async Task RemoveAsync()
    {
        await State.ClearStateAsync();
    }

    /// <inheritdoc />
    public async Task RenewAsync(
        DateTimeOffset expiresAt
    )
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return;
        }

        State.State.Data = State.State.Data with
        {
            ExpiresAt = expiresAt,
            LastRenewedAt = TimeProvider.GetUtcNow(),
        };
        await State.WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task StoreAsync(
        AuthTicketData data
    )
    {
        State.State = new()
        {
            Data = data,
        };
        await State.WriteStateAsync();
    }

    [GenerateSerializer]
    [Alias("Mississippi.AspNetCore.Orleans.Authentication.AuthTicketState")]
    internal sealed record AuthTicketState
    {
        [Id(0)]
        public AuthTicketData? Data { get; set; }
    }
}