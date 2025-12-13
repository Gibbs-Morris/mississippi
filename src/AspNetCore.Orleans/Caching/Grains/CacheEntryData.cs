namespace Mississippi.AspNetCore.Orleans.Caching.Grains;

using System;
using global::Orleans;

/// <summary>
/// Represents cached data with expiration metadata.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.AspNetCore.Orleans.Caching.CacheEntryData")]
public sealed record CacheEntryData
{
    /// <summary>
    /// Gets the cached value bytes.
    /// </summary>
    [Id(0)]
    public byte[] Value { get; init; } = [];

    /// <summary>
    /// Gets the absolute expiration time, if set.
    /// </summary>
    [Id(1)]
    public DateTimeOffset? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets the sliding expiration duration, if set.
    /// </summary>
    [Id(2)]
    public TimeSpan? SlidingExpiration { get; init; }

    /// <summary>
    /// Gets the last access time for sliding expiration.
    /// </summary>
    [Id(3)]
    public DateTimeOffset LastAccessTime { get; init; }
}
