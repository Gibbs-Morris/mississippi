namespace Mississippi.Core.Abstractions.Providers.Projection;

/// <summary>
///     Provides write access to projection storage.
/// </summary>
public interface IProjectionStorageWriter
{
    /// <summary>
    ///     Writes a projection to storage.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection to write.</typeparam>
    /// <param name="key">The key identifying the projection.</param>
    /// <param name="version">The version of the projection to write.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteAsync<TProjection>(
        string key,
        long version,
        CancellationToken cancellationToken = default
    );
}
