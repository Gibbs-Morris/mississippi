using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a stateless Orleans grain that exposes a read-only, cached view of a projection
///     for a specific path, identifier, and version.
/// </summary>
/// <typeparam name="TModel">The projection model type exposed by the grain.</typeparam>
/// <remarks>
///     The grain key is a string that encodes the projection path, aggregate identifier, and version,
///     allowing callers to retrieve a specific version of the projection without owning its lifecycle.
/// </remarks>
[Alias("Mississippi.Projections.IProjectionGrain`1")]
public interface IProjectionGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current projection model and associated metadata for the grain key.
    /// </summary>
    /// <returns>
    ///     A task that, when completed successfully, contains an immutable
    ///     <see cref="ProjectionResult{TModel}" /> for the projection identified by this grain.
    /// </returns>
    [Alias("GetAsync")]
    Task<ProjectionResult<TModel>> GetAsync();
}