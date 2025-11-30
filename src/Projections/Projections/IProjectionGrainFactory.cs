namespace Mississippi.Projections.Projections;

/// <summary>
///     Defines a factory for resolving projection Orleans grains by strongly typed keys.
/// </summary>
public interface IProjectionGrainFactory
{
    /// <summary>
    ///     Resolves an <see cref="IProjectionBuilderGrain{TModel}" /> for the specified versioned projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type produced by the builder.</typeparam>
    /// <param name="key">The versioned projection key identifying the builder instance.</param>
    /// <returns>An Orleans grain that constructs projection models on demand.</returns>
    IProjectionBuilderGrain<TModel> GetProjectionBuilderGrain<TModel>(
        VersionedProjectionKey key
    );

    /// <summary>
    ///     Resolves an <see cref="IProjectionCursorGrain" /> for the specified projection key.
    /// </summary>
    /// <param name="key">The projection key identifying the cursor.</param>
    /// <returns>An Orleans grain that exposes the projection cursor position.</returns>
    IProjectionCursorGrain GetProjectionCursorGrain(
        ProjectionKey key
    );

    /// <summary>
    ///     Resolves an <see cref="IProjectionGrain{TModel}" /> for the specified projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type served by the grain.</typeparam>
    /// <param name="key">The projection key identifying the projection instance.</param>
    /// <returns>An Orleans grain that exposes the projection model.</returns>
    IProjectionGrain<TModel> GetProjectionGrain<TModel>(
        ProjectionKey key
    );

    /// <summary>
    ///     Resolves an <see cref="IProjectionSnapshotGrain{TModel}" /> for the specified versioned projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type managed by the snapshot grain.</typeparam>
    /// <param name="key">The versioned projection key identifying the snapshot instance.</param>
    /// <returns>An Orleans grain that owns the projection snapshot.</returns>
    IProjectionSnapshotGrain<TModel> GetProjectionSnapshotGrain<TModel>(
        VersionedProjectionKey key
    );
}