using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples;

/// <summary>
///     Central state container for ripple projections using Redux-like dispatch pattern.
/// </summary>
public sealed class RippleStore : IRippleStore
{
    private readonly List<Action> listeners = [];

    private readonly object listenersLock = new();

    private readonly ConcurrentDictionary<(Type, string), object> states = new();

    private bool disposed;

    private static object CreateErrorState(
        Type stateType,
        string entityId,
        Exception error
    )
    {
        ConstructorInfo constructor = stateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([entityId]);
        newState = SetStateProperty(stateType, newState, "LastError", error);
        newState = SetStateProperty(stateType, newState, "IsLoading", false);
        newState = SetStateProperty(stateType, newState, "IsLoaded", false);
        return newState;
    }

    private static object CreateLoadedState(
        Type stateType,
        string entityId,
        object? data,
        long version
    )
    {
        ConstructorInfo constructor = stateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([entityId]);
        newState = SetStateProperty(stateType, newState, "Data", data);
        newState = SetStateProperty(stateType, newState, "Version", version);
        newState = SetStateProperty(stateType, newState, "IsLoaded", true);
        newState = SetStateProperty(stateType, newState, "IsLoading", false);
        return newState;
    }

    private static object CreateStateWithProperty(
        Type stateType,
        string entityId,
        bool isLoading
    )
    {
        ConstructorInfo constructor = stateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([entityId]);
        return SetStateProperty(stateType, newState, "IsLoading", isLoading);
    }

    private static object SetStateProperty(
        Type stateType,
        object state,
        string propertyName,
        object? value
    )
    {
        // Records use with-expressions which compile to a clone method
        // For now, use init-only property setters via reflection
        PropertyInfo prop = stateType.GetProperty(propertyName)!;

        // Records with init setters require special handling
        // Use the with-expression pattern by creating a new instance
        MethodInfo? cloneMethod = stateType.GetMethod("<Clone>$");
        if (cloneMethod != null)
        {
            object clone = cloneMethod.Invoke(state, null)!;
            prop.SetValue(clone, value);
            return clone;
        }

        // Fallback: direct set (may not work with readonly props)
        prop.SetValue(state, value);
        return state;
    }

    /// <inheritdoc />
    public void Dispatch(
        IRippleAction action
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(action);
        ReduceAction(action);
        NotifyListeners();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lock (listenersLock)
        {
            listeners.Clear();
        }

        states.Clear();
    }

    /// <inheritdoc />
    public IProjectionState<T>? GetState<T>(
        string entityId
    )
        where T : class
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        (Type, string) key = (typeof(T), entityId);
        if (states.TryGetValue(key, out object? state))
        {
            return (IProjectionState<T>)state;
        }

        return null;
    }

    /// <inheritdoc />
    public IDisposable Subscribe(
        Action listener
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(listener);
        lock (listenersLock)
        {
            listeners.Add(listener);
        }

        return new Subscription(this, listener);
    }

    private void NotifyListeners()
    {
        List<Action> snapshot;
        lock (listenersLock)
        {
            snapshot = [.. listeners];
        }

        foreach (Action listener in snapshot)
        {
            listener();
        }
    }

    private void ReduceAction(
        IRippleAction action
    )
    {
        switch (action)
        {
            case ProjectionLoadingAction<object> loading:
                ReduceLoadingAction(loading);
                break;
            case ProjectionLoadedAction<object> loaded:
                ReduceLoadedAction(loaded);
                break;
            case ProjectionErrorAction<object> error:
                ReduceErrorAction(error);
                break;
            case ProjectionConnectionChangedAction<object> connectionChanged:
                ReduceConnectionChangedAction(connectionChanged);
                break;
            default:
                ReduceGenericAction(action);
                break;
        }
    }

    private void ReduceConnectionChangedAction<T>(
        ProjectionConnectionChangedAction<T> action
    )
        where T : class
    {
        (Type, string EntityId) key = (typeof(T), action.EntityId);
        IProjectionState<T>? existingState = GetState<T>(action.EntityId);
        if (existingState is ProjectionState<T> state)
        {
            ProjectionState<T> newState = state with
            {
                IsConnected = action.IsConnected,
            };
            states[key] = newState;
        }
    }

    private void ReduceConnectionChangedGeneric(
        Type projectionType,
        IRippleAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo isConnectedProp = actionType.GetProperty("IsConnected")!;
        bool isConnected = (bool)isConnectedProp.GetValue(action)!;
        (Type, string) key = (projectionType, action.EntityId);
        if (states.TryGetValue(key, out object? existingState))
        {
            Type stateType = existingState.GetType();
            object withIsConnected = SetStateProperty(stateType, existingState, "IsConnected", isConnected);
            states[key] = withIsConnected;
        }
    }

    private void ReduceErrorAction<T>(
        ProjectionErrorAction<T> action
    )
        where T : class
    {
        (Type, string EntityId) key = (typeof(T), action.EntityId);
        IProjectionState<T>? existingState = GetState<T>(action.EntityId);
        ProjectionState<T> newState = new(action.EntityId)
        {
            Data = existingState?.Data,
            Version = existingState?.Version ?? 0,
            IsLoading = false,
            IsLoaded = false,
            LastError = action.Error,
        };
        states[key] = newState;
    }

    private void ReduceErrorGeneric(
        Type projectionType,
        IRippleAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo errorProp = actionType.GetProperty("Error")!;
        Exception error = (Exception)errorProp.GetValue(action)!;
        (Type, string) key = (projectionType, action.EntityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        object newState = CreateErrorState(stateType, action.EntityId, error);
        states[key] = newState;
    }

    private void ReduceGenericAction(
        IRippleAction action
    )
    {
        Type actionType = action.GetType();
        if (!actionType.IsGenericType)
        {
            return;
        }

        Type genericDef = actionType.GetGenericTypeDefinition();
        Type projectionType = actionType.GetGenericArguments()[0];
        if (genericDef == typeof(ProjectionLoadingAction<>))
        {
            ReduceLoadingGeneric(projectionType, action.EntityId);
        }
        else if (genericDef == typeof(ProjectionLoadedAction<>))
        {
            ReduceLoadedGeneric(projectionType, action);
        }
        else if (genericDef == typeof(ProjectionErrorAction<>))
        {
            ReduceErrorGeneric(projectionType, action);
        }
        else if (genericDef == typeof(ProjectionConnectionChangedAction<>))
        {
            ReduceConnectionChangedGeneric(projectionType, action);
        }
    }

    private void ReduceLoadedAction<T>(
        ProjectionLoadedAction<T> action
    )
        where T : class
    {
        (Type, string EntityId) key = (typeof(T), action.EntityId);
        ProjectionState<T> newState = new(action.EntityId)
        {
            Data = action.Data,
            Version = action.Version,
            IsLoaded = true,
            IsLoading = false,
        };
        states[key] = newState;
    }

    private void ReduceLoadedGeneric(
        Type projectionType,
        IRippleAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo dataProp = actionType.GetProperty("Data")!;
        PropertyInfo versionProp = actionType.GetProperty("Version")!;
        object? data = dataProp.GetValue(action);
        long version = (long)versionProp.GetValue(action)!;
        (Type, string) key = (projectionType, action.EntityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        object newState = CreateLoadedState(stateType, action.EntityId, data, version);
        states[key] = newState;
    }

    private void ReduceLoadingAction<T>(
        ProjectionLoadingAction<T> action
    )
        where T : class
    {
        (Type, string EntityId) key = (typeof(T), action.EntityId);
        ProjectionState<T> newState = new(action.EntityId)
        {
            IsLoading = true,
        };
        states[key] = newState;
    }

    private void ReduceLoadingGeneric(
        Type projectionType,
        string entityId
    )
    {
        (Type, string) key = (projectionType, entityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);

        // Use reflection to create with-expression equivalent
        object withIsLoading = CreateStateWithProperty(stateType, entityId, true);
        states[key] = withIsLoading;
    }

    private void Unsubscribe(
        Action listener
    )
    {
        lock (listenersLock)
        {
            listeners.Remove(listener);
        }
    }

    /// <summary>
    ///     Represents a subscription to store state changes that can be disposed to unsubscribe.
    /// </summary>
    private sealed class Subscription : IDisposable
    {
        private Action? listener;

        private RippleStore? store;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="store">The store to subscribe to.</param>
        /// <param name="listener">The listener callback to invoke on state changes.</param>
        public Subscription(
            RippleStore store,
            Action listener
        )
        {
            this.store = store;
            this.listener = listener;
        }

        /// <summary>
        ///     Unsubscribes the listener from the store and releases references.
        /// </summary>
        public void Dispose()
        {
            store?.Unsubscribe(listener!);
            store = null;
            listener = null;
        }
    }
}