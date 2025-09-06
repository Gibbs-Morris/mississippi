namespace Mississippi.Core.Abstractions.Providers.Projection;

/// <summary>
///     Provides read access to projection storage.
/// </summary>
public interface IProjectionStorageReader
{
    /// <summary>
    ///     Reads a projection from storage.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection to read.</typeparam>
    /// <param name="key">The key identifying the projection.</param>
    /// <param name="version">The version of the projection to read.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The projection instance.</returns>
    Task<TProjection> ReadAsync<TProjection>(
        string key,
        long version,
        CancellationToken cancellationToken = default
    );
}