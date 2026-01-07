using System;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Notifier that receives projection updates from the server and dispatches actions.
/// </summary>
public interface IProjectionUpdateNotifier
{
    /// <summary>
    ///     Notifies the store that a projection connection state has changed.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="isConnected">Whether the projection is connected.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void NotifyConnectionChanged<T>(
        string entityId,
        bool isConnected
    )
        where T : class;

    /// <summary>
    ///     Notifies the store that a projection error has occurred.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="exception">The error that occurred.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> or <paramref name="exception" /> is null.
    /// </exception>
    void NotifyError<T>(
        string entityId,
        Exception exception
    )
        where T : class;

    /// <summary>
    ///     Notifies the store that a projection has been updated.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The updated data.</param>
    /// <param name="version">The server version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    void NotifyProjectionUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class;
}