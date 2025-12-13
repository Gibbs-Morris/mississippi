namespace Mississippi.AspNetCore.Orleans.Caching.Grains;

using System;
using System.Threading.Tasks;
using global::Orleans;

/// <summary>
/// Grain interface for distributed cache entry storage.
/// Each grain represents a single cache entry keyed by the cache key.
/// </summary>
public interface ICacheEntryGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the cached value and metadata.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation containing the cache entry data,
    /// or null if the entry does not exist or has expired.
    /// </returns>
    Task<CacheEntryData?> GetAsync();

    /// <summary>
    /// Sets the cached value with expiration metadata.
    /// </summary>
    /// <param name="data">The cache entry data to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(CacheEntryData data);

    /// <summary>
    /// Removes the cached value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync();

    /// <summary>
    /// Refreshes the sliding expiration window for the cached value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshAsync();
}
