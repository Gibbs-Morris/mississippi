using System;

using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.Abstractions;

/// <summary>
///     Read-only cache for server-synced projection states.
/// </summary>
/// <remarks>
///     <para>
///         Projection data is managed through Redux actions and reducers.
///         This interface provides read-only access to the current state.
///     </para>
///     <para>
///         To update projections, dispatch the appropriate action:
///         <see cref="Actions.RefreshProjectionAction{T}" />,
///         <see cref="Actions.SubscribeToProjectionAction{T}" />, etc.
///     </para>
/// </remarks>
public interface IProjectionCache
{
    /// <summary>
    ///     Gets the projection data for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection data, or null if not loaded.</returns>
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