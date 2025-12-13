namespace Mississippi.AspNetCore.Orleans.OutputCaching.Options;

using System;

/// <summary>
/// Configuration options for Orleans-backed output cache.
/// </summary>
public sealed class OrleansOutputCacheOptions
{
    /// <summary>
    /// Gets or sets the key prefix for output cache entries.
    /// Default: "output:".
    /// </summary>
    public string KeyPrefix { get; set; } = "output:";

    /// <summary>
    /// Gets or sets the default cache duration.
    /// Default: 60 seconds.
    /// </summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(60);
}
