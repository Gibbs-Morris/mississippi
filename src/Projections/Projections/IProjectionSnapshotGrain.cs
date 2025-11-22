using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a stateful Orleans grain that owns and persists a versioned snapshot of a projection
///     for a specific path and identifier.
/// </summary>
/// <typeparam name="TModel">The projection model type managed by the grain.</typeparam>
/// <remarks>
///     The grain key is a string that encodes the projection path and aggregate identifier. The grain
///     maintains the current snapshot and version, updating its state as new events are applied.
/// </remarks>
[Alias("Mississippi.Projections.IProjectionSnapshotGrain`1")]
public interface IProjectionSnapshotGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Builds or rebuilds the projection snapshot for the grain key ahead of use, for example
    ///     to warm up the grain before it begins serving read requests.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous build operation.
    /// </returns>
    [Alias("BuildAsync")]
    Task BuildAsync();

    /// <summary>
    ///     Gets the current projection snapshot and associated metadata for the grain key.
    /// </summary>
    /// <returns>
    ///     A task that, when completed successfully, contains an immutable
    ///     <see cref="ProjectionResult{TModel}" /> representing the latest persisted snapshot
    ///     for this projection.
    /// </returns>
    [Alias("GetAsync")]
    Task<ProjectionResult<TModel>> GetAsync();
}