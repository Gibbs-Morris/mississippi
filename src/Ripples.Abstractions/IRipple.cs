namespace Mississippi.Ripples.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides reactive access to a UX projection with automatic updates.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public interface IRipple<T> : IAsyncDisposable
    where T : class
{
    /// <summary>
    /// Gets the current projection data.
    /// </summary>
    T? Current { get; }

    /// <summary>
    /// Gets the current version from the server.
    /// </summary>
    long? Version { get; }

    /// <summary>
    /// Gets a value indicating whether data is being fetched.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets a value indicating whether the first successful load has completed.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets a value indicating whether connected to real-time updates.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the last error that occurred, if any.
    /// </summary>
    Exception? LastError { get; }

    /// <summary>
    /// Raised when any property changes.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Raised when an error occurs.
    /// </summary>
    event EventHandler<RippleErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Subscribe to a projection entity.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SubscribeAsync(string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from the current entity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnsubscribeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Force refresh from source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
