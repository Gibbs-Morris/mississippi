using System;

using Microsoft.Extensions.Logging;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Factory for resolving projection-related Orleans grains by projection key.
/// </summary>
internal sealed class ProjectionGrainFactory(IGrainFactory grainFactory, ILogger<ProjectionGrainFactory> logger)
    : IProjectionGrainFactory
{
    private IGrainFactory GrainFactory { get; } = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));

    private ILogger<ProjectionGrainFactory> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Retrieves an <see cref="IProjectionBuilderGrain{TModel}" /> for the specified versioned projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="key">The versioned projection key identifying the builder.</param>
    /// <returns>The resolved builder grain.</returns>
    public IProjectionBuilderGrain<TModel> GetProjectionBuilderGrain<TModel>(
        VersionedProjectionKey key
    )
    {
        Logger.ResolvingProjectionBuilderGrain<TModel>(key);
        return GrainFactory.GetGrain<IProjectionBuilderGrain<TModel>>(key);
    }

    /// <summary>
    ///     Retrieves an <see cref="IProjectionCursorGrain" /> for the specified projection key.
    /// </summary>
    /// <param name="key">The projection key identifying the cursor.</param>
    /// <returns>The resolved cursor grain.</returns>
    public IProjectionCursorGrain GetProjectionCursorGrain(
        ProjectionKey key
    )
    {
        Logger.ResolvingProjectionCursorGrain(key);
        return GrainFactory.GetGrain<IProjectionCursorGrain>(key);
    }

    /// <summary>
    ///     Retrieves an <see cref="IProjectionGrain{TModel}" /> for the specified projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="key">The projection key identifying the projection instance.</param>
    /// <returns>The resolved projection grain.</returns>
    public IProjectionGrain<TModel> GetProjectionGrain<TModel>(
        ProjectionKey key
    )
    {
        Logger.ResolvingProjectionGrain<TModel>(key);
        return GrainFactory.GetGrain<IProjectionGrain<TModel>>(key);
    }

    /// <summary>
    ///     Retrieves an <see cref="IProjectionSnapshotGrain{TModel}" /> for the specified versioned projection key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="key">The versioned projection key identifying the snapshot.</param>
    /// <returns>The resolved snapshot grain.</returns>
    public IProjectionSnapshotGrain<TModel> GetProjectionSnapshotGrain<TModel>(
        VersionedProjectionKey key
    )
    {
        Logger.ResolvingProjectionSnapshotGrain<TModel>(key);
        return GrainFactory.GetGrain<IProjectionSnapshotGrain<TModel>>(key);
    }
}