using System.Threading.Tasks;

using Mississippi.EventSourcing.Abstractions;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Brooks.Cursor;

/// <summary>
///     Orleans grain contract that exposes the cursor (latest version) of a Mississippi brook.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" /> to provide
///     a unique identifier for each Mississippi brook.
/// </remarks>
[Alias("Mississippi.Core.IBrookCursorGrain")]
public interface IBrookCursorGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Requests the grain to deactivate when idle, clearing any in-memory caches.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    [Alias("DeactivateAsync")]
    Task DeactivateAsync();

    /// <summary>
    ///     Retrieves the most recent version number of the associated brook.
    /// </summary>
    /// <returns>The highest persisted version of the brook.</returns>
    [ReadOnly]
    [Alias("GetLatestPositionAsync")]
    Task<BrookPosition> GetLatestPositionAsync();

    /// <summary>
    ///     Retrieves the most recent version number by forcing a storage read, bypassing cache.
    ///     Use when a strongly confirmed position is required.
    /// </summary>
    /// <returns>The highest persisted version of the brook from storage.</returns>
    [Alias("GetLatestPositionConfirmedAsync")]
    Task<BrookPosition> GetLatestPositionConfirmedAsync();
}