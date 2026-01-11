using System;


namespace Cascade.Web.Contracts;

/// <summary>
///     Response from the health endpoint.
/// </summary>
public sealed record HealthResponse
{
    /// <summary>
    ///     Gets the health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    ///     Gets the timestamp of the health check.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
