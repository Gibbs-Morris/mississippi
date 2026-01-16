using System;

using Mississippi.Inlet.Abstractions.State;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Cache for server-synced projection states.
/// </summary>
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

    /// <summary>
    ///     Sets the connection state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="isConnected">Whether the projection is connected.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void SetConnection<T>(
        string entityId,
        bool isConnected
    )
        where T : class;

    /// <summary>
    ///     Sets the error state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="exception">The error that occurred.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> or <paramref name="exception" /> is null.
    /// </exception>
    void SetError<T>(
        string entityId,
        Exception exception
    )
        where T : class;

    /// <summary>
    ///     Sets the loaded state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The loaded data.</param>
    /// <param name="version">The server version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void SetLoaded<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class;

    /// <summary>
    ///     Sets the loading state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void SetLoading<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Sets the updated state for a projection.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The updated data.</param>
    /// <param name="version">The server version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void SetUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class;
}