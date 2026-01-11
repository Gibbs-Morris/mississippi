using System;

using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     An extended store that manages server-synced projections in addition to local state.
/// </summary>
public interface IInletStore : IStore
{
    /// <summary>
    ///     Gets the projection state for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection state, or null if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    T? GetProjection<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Gets the error for a projection, if any.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The error, or null if no error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Gets the full projection state for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection state, or null if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Gets the version of a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The server version, or -1 if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    long GetProjectionVersion<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Gets the connection state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is connected to the server.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Gets the loading state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is currently loading.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class;
}