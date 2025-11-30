using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Projections.Projections;

/// <summary>
///     High-performance logging extensions for <see cref="ProjectionGrainFactory" /> operations.
///     Public visibility satisfies repository logging rules that require globally accessible logger extensions.
/// </summary>
public static class ProjectionGrainFactoryLoggerExtensions
{
    private static readonly Action<ILogger, string, ProjectionKey, Exception?> ResolvingProjectionGrainMessage =
        LoggerMessage.Define<string, ProjectionKey>(
            LogLevel.Debug,
            new(1, nameof(ResolvingProjectionGrain)),
            "Resolving {GrainType} for Projection {ProjectionKey}");

    private static readonly Action<ILogger, string, ProjectionKey, Exception?> ResolvingProjectionCursorGrainMessage =
        LoggerMessage.Define<string, ProjectionKey>(
            LogLevel.Debug,
            new(2, nameof(ResolvingProjectionCursorGrain)),
            "Resolving {GrainType} for Projection {ProjectionKey}");

    private static readonly Action<ILogger, string, VersionedProjectionKey, Exception?>
        ResolvingProjectionSnapshotGrainMessage = LoggerMessage.Define<string, VersionedProjectionKey>(
            LogLevel.Debug,
            new(3, nameof(ResolvingProjectionSnapshotGrain)),
            "Resolving {GrainType} for Projection {VersionedProjectionKey}");

    private static readonly Action<ILogger, string, VersionedProjectionKey, Exception?>
        ResolvingProjectionBuilderGrainMessage = LoggerMessage.Define<string, VersionedProjectionKey>(
            LogLevel.Debug,
            new(4, nameof(ResolvingProjectionBuilderGrain)),
            "Resolving {GrainType} for Projection {VersionedProjectionKey}");

    /// <summary>
    ///     Logs that a projection builder grain is being resolved for the provided key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The versioned projection key.</param>
    internal static void ResolvingProjectionBuilderGrain<TModel>(
        this ILogger<ProjectionGrainFactory> logger,
        VersionedProjectionKey key
    )
    {
        ResolvingProjectionBuilderGrainMessage(logger, nameof(IProjectionBuilderGrain<TModel>), key, null);
    }

    /// <summary>
    ///     Logs that a projection cursor grain is being resolved for the provided key.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The projection key.</param>
    internal static void ResolvingProjectionCursorGrain(
        this ILogger<ProjectionGrainFactory> logger,
        ProjectionKey key
    )
    {
        ResolvingProjectionCursorGrainMessage(logger, nameof(IProjectionCursorGrain), key, null);
    }

    /// <summary>
    ///     Logs that a projection grain is being resolved for the provided key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The projection key.</param>
    internal static void ResolvingProjectionGrain<TModel>(
        this ILogger<ProjectionGrainFactory> logger,
        ProjectionKey key
    )
    {
        ResolvingProjectionGrainMessage(logger, nameof(IProjectionGrain<TModel>), key, null);
    }

    /// <summary>
    ///     Logs that a projection snapshot grain is being resolved for the provided key.
    /// </summary>
    /// <typeparam name="TModel">The projection model type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The versioned projection key.</param>
    internal static void ResolvingProjectionSnapshotGrain<TModel>(
        this ILogger<ProjectionGrainFactory> logger,
        VersionedProjectionKey key
    )
    {
        ResolvingProjectionSnapshotGrainMessage(logger, nameof(IProjectionSnapshotGrain<TModel>), key, null);
    }
}