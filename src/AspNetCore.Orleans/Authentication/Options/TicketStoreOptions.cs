namespace Mississippi.AspNetCore.Orleans.Authentication.Options;

using System;

/// <summary>
/// Configuration options for Orleans-backed ticket store.
/// </summary>
public sealed class TicketStoreOptions
{
    /// <summary>
    /// Gets or sets the key prefix for ticket entries.
    /// Default: "ticket:".
    /// </summary>
    public string KeyPrefix { get; set; } = "ticket:";

    /// <summary>
    /// Gets or sets the default expiration duration for tickets.
    /// Default: 14 days.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromDays(14);
}
