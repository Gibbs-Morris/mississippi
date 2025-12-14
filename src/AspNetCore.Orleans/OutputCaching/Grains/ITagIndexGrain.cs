using System.Collections.Generic;
using System.Threading.Tasks;

using Orleans;


namespace Mississippi.AspNetCore.Orleans.OutputCaching.Grains;

/// <summary>
///     Grain interface for tracking cache keys by tag for efficient tag-based eviction.
/// </summary>
public interface ITagIndexGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Adds a key to this tag's index.
    /// </summary>
    /// <param name="key">The cache key to associate with this tag.</param>
    Task AddKeyAsync(
        string key
    );

    /// <summary>
    ///     Gets all keys associated with this tag.
    /// </summary>
    /// <returns>A collection of cache keys that have this tag.</returns>
    Task<IReadOnlyList<string>> GetKeysAsync();

    /// <summary>
    ///     Removes a key from this tag's index.
    /// </summary>
    /// <param name="key">The cache key to remove from this tag.</param>
    Task RemoveKeyAsync(
        string key
    );
}
