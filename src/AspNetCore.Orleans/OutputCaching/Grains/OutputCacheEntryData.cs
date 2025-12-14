using System;

using Orleans;


namespace Mississippi.AspNetCore.Orleans.OutputCaching.Grains;

/// <summary>
///     Represents output cache entry data.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.AspNetCore.Orleans.OutputCaching.OutputCacheEntryData")]
public sealed record OutputCacheEntryData
{
    /// <summary>
    ///     Gets the cached response body.
    /// </summary>
    [Id(0)]
    public byte[] Body { get; init; } = [];

    /// <summary>
    ///     Gets the expiration time.
    /// </summary>
    [Id(1)]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    ///     Gets the tags associated with this entry.
    /// </summary>
    [Id(2)]
    public string[] Tags { get; init; } = [];
}