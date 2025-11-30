using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a stateless Orleans grain that exposes the current cursor position
///     for a projection stream identified by the grain key.
/// </summary>
/// <remarks>
///     The grain key is a string that encodes the projection path and aggregate identifier,
///     allowing callers to obtain the latest processed position without owning the projection
///     or its lifecycle.
/// </remarks>
[Alias("Mississippi.Projections.IProjectionCursorGrain")]
public interface IProjectionCursorGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current cursor position associated with the grain key.
    /// </summary>
    /// <returns>
    ///     A task that, when completed successfully, contains the latest processed
    ///     position as a zero-based <see cref="long" /> value.
    /// </returns>
    [Alias("GetCursorPositionAsync")]
    Task<long> GetCursorPositionAsync();
}