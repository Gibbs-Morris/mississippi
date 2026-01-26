using System;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Notifier that dispatches projection update actions to the store.
///     The <see cref="ProjectionCacheMiddleware" /> intercepts these actions
///     and updates the <see cref="IProjectionCache" />.
/// </summary>
public sealed class ProjectionNotifier : IProjectionUpdateNotifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionNotifier" /> class.
    /// </summary>
    /// <param name="store">The store to dispatch actions to.</param>
    /// <param name="projectionCache">The projection cache to update for connection/error states.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> or <paramref name="projectionCache" /> is null.
    /// </exception>
    public ProjectionNotifier(
        IStore store,
        IProjectionCache projectionCache
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(projectionCache);
        Store = store;
        ProjectionCache = projectionCache;
    }

    private IProjectionCache ProjectionCache { get; }

    private IStore Store { get; }

    /// <inheritdoc />
    public void NotifyConnectionChanged<T>(
        string entityId,
        bool isConnected
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionCache.SetConnection<T>(entityId, isConnected);
        Store.Dispatch(new ProjectionConnectionChangedAction<T>(entityId, isConnected));
    }

    /// <inheritdoc />
    public void NotifyError<T>(
        string entityId,
        Exception exception
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(exception);
        ProjectionCache.SetError<T>(entityId, exception);
        Store.Dispatch(new ProjectionErrorAction<T>(entityId, exception));
    }

    /// <inheritdoc />
    public void NotifyProjectionUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionCache.SetUpdated<T>(entityId, data, version);
        Store.Dispatch(new ProjectionUpdatedAction<T>(entityId, data, version));
    }
}
