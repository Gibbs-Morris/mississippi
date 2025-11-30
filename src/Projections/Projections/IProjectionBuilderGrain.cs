using System.Threading.Tasks;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a stateless Orleans grain that builds or rebuilds a projection instance for a
///     specific path and identifier by applying one or more projection reducers.
/// </summary>
/// <typeparam name="TModel">The projection model type produced by the grain.</typeparam>
/// <remarks>
///     The grain key is a string that encodes the projection path and aggregate identifier. The grain
///     coordinates the application of projection reducers but does not persist long-lived state.
/// </remarks>
[Alias("Mississippi.Projections.IProjectionBuilderGrain`1")]
public interface IProjectionBuilderGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Builds a projection for the grain key by applying the configured projection reducers
    ///     and returns the resulting model together with identifying metadata.
    /// </summary>
    /// <returns>
    ///     A task that, when completed successfully, contains an immutable
    ///     <see cref="ProjectionResult{TModel}" /> representing the built projection for this grain.
    /// </returns>
    [Alias("BuildAsync")]
    Task<ProjectionResult<TModel>> BuildAsync();
}