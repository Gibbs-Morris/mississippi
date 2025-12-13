namespace Mississippi.AspNetCore.Orleans.SignalR.Grains;

using global::Orleans;

/// <summary>
/// Represents SignalR connection data.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.AspNetCore.Orleans.SignalR.ConnectionData")]
public sealed record ConnectionData
{
    /// <summary>
    /// Gets the connection identifier.
    /// </summary>
    [Id(0)]
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user identifier, if authenticated.
    /// </summary>
    [Id(1)]
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the groups the connection belongs to.
    /// </summary>
    [Id(2)]
    public string[] Groups { get; init; } = [];
}
