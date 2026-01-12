using System;
using System.Collections.Concurrent;
using System.Reflection;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Inlet.State;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet;

/// <summary>
///     Store that extends the base Reservoir store with server-synced projection support.
/// </summary>
public class InletStore
    : Store,
      IInletStore,
      IProjectionUpdateNotifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletStore" /> class.
    /// </summary>
    public InletStore()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletStore" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public InletStore(
        IServiceProvider serviceProvider
    )
        : base(serviceProvider)
    {
    }

    private ConcurrentDictionary<ProjectionKey, object> ProjectionStates { get; } = new();

    /// <summary>
    ///     Creates a new InletStore with the specified effect.
    ///     The store takes ownership of the effect and will dispose it when the store is disposed.
    /// </summary>
    /// <typeparam name="TEffect">The effect type.</typeparam>
    /// <param name="effectFactory">Factory function to create the effect. Receives the store instance.</param>
    /// <returns>A new InletStore with the effect registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="effectFactory" /> is null.</exception>
    public static InletStore CreateWithEffect<TEffect>(
        Func<InletStore, TEffect> effectFactory
    )
        where TEffect : IEffect
    {
        ArgumentNullException.ThrowIfNull(effectFactory);
        InletStore store = new();
        TEffect effect = effectFactory(store);
        store.RegisterEffect(effect);
        return store;
    }

    private static bool IsGenericActionOfType(
        Type actualType,
        Type genericDefinition
    ) =>
        actualType.IsGenericType && (actualType.GetGenericTypeDefinition() == genericDefinition);

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
    public void NotifyConnectionChanged<T>(
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
        Dispatch(new ProjectionConnectionChangedAction<T>(entityId, isConnected));
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
        Dispatch(new ProjectionErrorAction<T>(entityId, exception));
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
        ProjectionKey key = new(typeof(T), entityId);
        ProjectionStates.AddOrUpdate(
            key,
            _ => new ProjectionState<T>(data, version, false, true),
            (
                _,
                existing
            ) =>
            {
                if (existing is ProjectionState<T> typedState)
                {
                    return typedState.WithData(data, version);
                }

                return new ProjectionState<T>(data, version, false, true);
            });
        Dispatch(new ProjectionUpdatedAction<T>(entityId, data, version));
    }

    /// <inheritdoc />
    protected override void OnActionDispatched(
        IAction action
    )
    {
        base.OnActionDispatched(action);
        HandleProjectionActions(action);
    }

    private void HandleLoadedAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        PropertyInfo? entityIdProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.EntityId));
        PropertyInfo? dataProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.Data));
        PropertyInfo? versionProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.Version));
        string? entityId = entityIdProperty?.GetValue(action) as string;
        object? data = dataProperty?.GetValue(action);
        long version = (long)(versionProperty?.GetValue(action) ?? -1L);
        if (entityId is null)
        {
            return;
        }

        ProjectionKey key = new(projectionType, entityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        ConstructorInfo? constructor = stateType.GetConstructor(
            [projectionType, typeof(long), typeof(bool), typeof(bool), typeof(Exception)]);
        object? newState = constructor?.Invoke([data, version, false, true, null]);
        if (newState is not null)
        {
            ProjectionStates[key] = newState;
        }
    }

    private void HandleLoadingAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        string? entityId = (action as IInletAction)?.EntityId;
        if (entityId is null)
        {
            return;
        }

        ProjectionKey key = new(projectionType, entityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        FieldInfo? notLoadedField = stateType.GetField(nameof(ProjectionState<object>.NotLoaded));
        object? baseState = notLoadedField?.GetValue(null);
        if (baseState is null)
        {
            return;
        }

        MethodInfo? withLoadingMethod = stateType.GetMethod(nameof(ProjectionState<object>.WithLoading));
        object? loadingState = withLoadingMethod?.Invoke(baseState, null);
        if (loadingState is not null)
        {
            ProjectionStates[key] = loadingState;
        }
    }

    private void HandleProjectionActions(
        IAction action
    )
    {
        switch (action)
        {
            case { } a when IsGenericActionOfType(a.GetType(), typeof(ProjectionLoadingAction<>)):
                HandleLoadingAction(action);
                break;
            case { } a when IsGenericActionOfType(a.GetType(), typeof(ProjectionLoadedAction<>)):
                HandleLoadedAction(action);
                break;
            case { } a when IsGenericActionOfType(a.GetType(), typeof(ProjectionUpdatedAction<>)):
                HandleUpdatedAction(action);
                break;
        }
    }

    private void HandleUpdatedAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        PropertyInfo? entityIdProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.EntityId));
        PropertyInfo? dataProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.Data));
        PropertyInfo? versionProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.Version));
        string? entityId = entityIdProperty?.GetValue(action) as string;
        object? data = dataProperty?.GetValue(action);
        long version = (long)(versionProperty?.GetValue(action) ?? -1L);
        if (entityId is null)
        {
            return;
        }

        ProjectionKey key = new(projectionType, entityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        ConstructorInfo? constructor = stateType.GetConstructor(
            [projectionType, typeof(long), typeof(bool), typeof(bool), typeof(Exception)]);
        object? newState = constructor?.Invoke([data, version, false, true, null]);
        if (newState is not null)
        {
            ProjectionStates[key] = newState;
        }
    }

    private readonly record struct ProjectionKey(Type ProjectionType, string EntityId);
}