using System;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet;

/// <summary>
///     Composite implementation of <see cref="IInletStore" /> that delegates to
///     separate <see cref="IStore" /> and <see cref="IProjectionCache" /> instances.
/// </summary>
/// <remarks>
///     This class provides a unified interface for components that need both
///     Redux-style state management and server-synced projections.
/// </remarks>
public sealed class CompositeInletStore : IInletStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeInletStore" /> class.
    /// </summary>
    /// <param name="store">The underlying Redux-style store.</param>
    /// <param name="projectionCache">The projection cache for server-synced state.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> or <paramref name="projectionCache" /> is null.
    /// </exception>
    public CompositeInletStore(
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
    public void Dispatch(
        IAction action
    ) =>
        Store.Dispatch(action);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Store is IDisposable disposableStore)
        {
            disposableStore.Dispose();
        }
    }

    /// <inheritdoc />
    public T? GetProjection<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.GetProjection<T>(entityId);

    /// <inheritdoc />
    public Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.GetProjectionError<T>(entityId);

    /// <inheritdoc />
    public IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.GetProjectionState<T>(entityId);

    /// <inheritdoc />
    public long GetProjectionVersion<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.GetProjectionVersion<T>(entityId);

    /// <inheritdoc />
    public TState GetState<TState>()
        where TState : class, IFeatureState =>
        Store.GetState<TState>();

    /// <inheritdoc />
    public bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.IsProjectionConnected<T>(entityId);

    /// <inheritdoc />
    public bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.IsProjectionLoading<T>(entityId);

    /// <inheritdoc />
    public void SetConnection<T>(
        string entityId,
        bool isConnected
    )
        where T : class =>
        ProjectionCache.SetConnection<T>(entityId, isConnected);

    /// <inheritdoc />
    public void SetError<T>(
        string entityId,
        Exception exception
    )
        where T : class =>
        ProjectionCache.SetError<T>(entityId, exception);

    /// <inheritdoc />
    public void SetLoaded<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class =>
        ProjectionCache.SetLoaded(entityId, data, version);

    /// <inheritdoc />
    public void SetLoading<T>(
        string entityId
    )
        where T : class =>
        ProjectionCache.SetLoading<T>(entityId);

    /// <inheritdoc />
    public void SetUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class =>
        ProjectionCache.SetUpdated(entityId, data, version);

    /// <inheritdoc />
    public IDisposable Subscribe(
        Action listener
    ) =>
        Store.Subscribe(listener);
}