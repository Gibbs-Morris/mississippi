namespace Mississippi.AspNetCore.Orleans.Caching.Options;

using System;

/// <summary>
/// Configuration options for Orleans-backed distributed cache.
/// </summary>
public sealed class DistributedCacheOptions
{
    /// <summary>
    /// Gets or sets the key prefix for cache entries.
    /// Default: "cache:".
    /// </summary>
    public string KeyPrefix { get; set; } = "cache:";

    /// <summary>
    /// Gets or sets the default absolute expiration relative to now.
    /// Default: null (no expiration).
    /// </summary>
    public TimeSpan? DefaultAbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the default sliding expiration.
    /// Default: null (no sliding expiration).
    /// </summary>
    public TimeSpan? DefaultSlidingExpiration { get; set; }
}
