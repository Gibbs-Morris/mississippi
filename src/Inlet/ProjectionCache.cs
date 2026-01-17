using System;
using System.Collections.Concurrent;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Inlet.State;


namespace Mississippi.Inlet;

/// <summary>
///     Thread-safe cache for server-synced projection states.
/// </summary>
public sealed class ProjectionCache : IProjectionCache
{
    private ConcurrentDictionary<ProjectionKey, object> ProjectionStates { get; } = new();

    /// <inheritdoc />
    public T? GetProjection<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState.Data;
        }

        return default;
    }

    /// <inheritdoc />
    public Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState.ErrorException;
        }

        return null;
    }

    /// <inheritdoc />
    public IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState;
        }

        return null;
    }

    /// <inheritdoc />
    public long GetProjectionVersion<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState.Version;
        }

        return -1;
    }

    /// <inheritdoc />
    public bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState.IsConnected;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        if (ProjectionStates.TryGetValue(key, out object? state) && state is ProjectionState<T> typedState)
        {
            return typedState.IsLoading;
        }

        return false;
    }

    /// <inheritdoc />
    public void SetConnection<T>(
        string entityId,
        bool isConnected
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        ProjectionStates.AddOrUpdate(
            key,
            _ => new ProjectionState<T>().WithConnection(isConnected),
            (
                _,
                existing
            ) =>
            {
                if (existing is ProjectionState<T> typedState)
                {
                    return typedState.WithConnection(isConnected);
                }

                return new ProjectionState<T>().WithConnection(isConnected);
            });
    }

    /// <inheritdoc />
    public void SetError<T>(
        string entityId,
        Exception exception
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(exception);
        ProjectionKey key = new(typeof(T), entityId);
        ProjectionStates.AddOrUpdate(
            key,
            _ => new ProjectionState<T>().WithError(exception),
            (
                _,
                existing
            ) =>
            {
                if (existing is ProjectionState<T> typedState)
                {
                    return typedState.WithError(exception);
                }

                return new ProjectionState<T>().WithError(exception);
            });
    }

    /// <inheritdoc />
    public void SetLoaded<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        ProjectionStates[key] = new ProjectionState<T>(data, version, false, true);
    }

    /// <inheritdoc />
    public void SetLoading<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionKey key = new(typeof(T), entityId);
        ProjectionStates[key] = ProjectionState<T>.NotLoaded.WithLoading();
    }

    /// <inheritdoc />
    public void SetUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class =>
        SetLoaded(entityId, data, version);

    private readonly record struct ProjectionKey(Type ProjectionType, string EntityId);
}