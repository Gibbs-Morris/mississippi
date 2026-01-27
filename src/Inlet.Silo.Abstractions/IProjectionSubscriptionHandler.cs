using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Inlet.Silo.Abstractions;

/// <summary>
///     Handler that processes projection subscription requests and manages server connections.
/// </summary>
public interface IProjectionSubscriptionHandler
{
    /// <summary>
    ///     Refreshes a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the refresh.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    Task RefreshAsync<T>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    ///     Subscribes to a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the subscription.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    Task SubscribeAsync<T>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    ///     Unsubscribes from a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the unsubscription.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    Task UnsubscribeAsync<T>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where T : class;
}