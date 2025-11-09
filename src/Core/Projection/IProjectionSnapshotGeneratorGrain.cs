using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Grain interface responsible for building projection snapshots.
///     Handles the computation-intensive task of generating snapshots from source data.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[Alias("Mississippi.Core.Projection.IProjectionSnapshotGeneratorGrain")]
public interface IProjectionSnapshotGeneratorGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Initiates a background build operation for the projection snapshot.
    ///     This operation does not return the snapshot data immediately.
    /// </summary>
    /// <returns>A task representing the asynchronous background build operation.</returns>
    [Alias("BackgroundBuildAsync")]
    Task BackgroundBuildAsync();

    /// <summary>
    ///     Builds a projection snapshot synchronously and returns the result.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the computed model data.</returns>
    [Alias("BuildAsync")]
    Task<Immutable<ProjectionSnapshot<TModel>>> BuildAsync();
}