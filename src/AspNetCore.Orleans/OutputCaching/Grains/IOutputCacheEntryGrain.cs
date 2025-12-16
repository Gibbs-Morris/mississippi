using System.Threading.Tasks;

using Orleans;


namespace Mississippi.AspNetCore.Orleans.OutputCaching.Grains;

/// <summary>
///     Grain interface for output cache entry storage.
/// </summary>
public interface IOutputCacheEntryGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Evicts the output cache entry.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EvictAsync();

    /// <summary>
    ///     Gets the output cache entry.
    /// </summary>
    /// <returns>
    ///     A task representing the asynchronous operation containing the output cache entry,
    ///     or null if not found.
    /// </returns>
    Task<OutputCacheEntryData?> GetAsync();

    /// <summary>
    ///     Checks if the entry has a specific tag.
    /// </summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns>A task representing the asynchronous operation with true if the tag exists.</returns>
    Task<bool> HasTagAsync(
        string tag
    );

    /// <summary>
    ///     Sets the output cache entry.
    /// </summary>
    /// <param name="data">The output cache entry data.</param>
    /// <param name="tags">Optional tags for eviction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(
        OutputCacheEntryData data,
        string[]? tags
    );
}