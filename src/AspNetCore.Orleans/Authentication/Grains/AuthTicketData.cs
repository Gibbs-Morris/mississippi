namespace Mississippi.AspNetCore.Orleans.Authentication.Grains;

using System;
using global::Orleans;

/// <summary>
/// Represents serialized authentication ticket data.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.AspNetCore.Orleans.Authentication.AuthTicketData")]
public sealed record AuthTicketData
{
    /// <summary>
    /// Gets the serialized ticket bytes.
    /// </summary>
    [Id(0)]
    public byte[] TicketBytes { get; init; } = [];

    /// <summary>
    /// Gets the expiration time.
    /// </summary>
    [Id(1)]
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets the last renewal time.
    /// </summary>
    [Id(2)]
    public DateTimeOffset? LastRenewedAt { get; init; }
}
