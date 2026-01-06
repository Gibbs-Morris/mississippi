using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples;

/// <summary>
///     Central state container implementing Redux-like dispatch pattern.
///     Supports both local feature states and server-synced projection states.
/// </summary>
public sealed class RippleStore : IRippleStore
{
    private readonly ConcurrentDictionary<string, IEffect> effects = new();

    private readonly ConcurrentDictionary<string, object> featureStates = new();

    private readonly List<Action> listeners = [];

    private readonly object listenersLock = new();

    private readonly List<IMiddleware> middlewares = [];

    private readonly ConcurrentDictionary<(Type, string), object> projectionStates = new();

    private readonly ConcurrentDictionary<string, object> rootActionReducers = new();

    private readonly IServiceProvider? serviceProvider;

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RippleStore" /> class.
    /// </summary>
    public RippleStore()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RippleStore" /> class with DI support.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The service provider for resolving root action reducers and effects.
    /// </param>
    public RippleStore(
        IServiceProvider serviceProvider
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this.serviceProvider = serviceProvider;

        // Resolve effects registered in DI
        foreach (IEffect effect in serviceProvider.GetServices<IEffect>())
        {
            RegisterEffect(effect);
        }

        // Resolve middleware registered in DI
        foreach (IMiddleware middleware in serviceProvider.GetServices<IMiddleware>())
        {
            RegisterMiddleware(middleware);
        }
    }

    /// <summary>
    ///     Creates a new RippleStore with the specified effects.
    ///     The store takes ownership of the effects and will dispose them when the store is disposed.
    /// </summary>
    /// <typeparam name="TEffect">The effect type.</typeparam>
    /// <param name="effectFactory">Factory function to create the effect. Receives the store instance.</param>
    /// <returns>A new RippleStore with the effect registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="effectFactory" /> is null.</exception>
    public static RippleStore CreateWithEffect<TEffect>(
        Func<RippleStore, TEffect> effectFactory
    )
        where TEffect : IEffect
    {
        ArgumentNullException.ThrowIfNull(effectFactory);
        RippleStore store = new();
        TEffect effect = effectFactory(store);
        store.RegisterEffect(effect);
        return store;
    }

    private static object SetStateProperty(
        Type stateType,
        object state,
        string propertyName,
        object? value
    )
    {
        PropertyInfo? prop = stateType.GetProperty(propertyName);
        if (prop == null)
        {
            return state;
        }

        // Records use with-expressions which compile to a clone method
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
        IAction action
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(action);

        // Build the middleware pipeline ending with the core dispatch
        Action<IAction> coreDispatch = CoreDispatch;
        Action<IAction> pipeline = BuildMiddlewarePipeline(coreDispatch);

        // Execute the pipeline
        pipeline(action);
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

        // Dispose any effects that implement IDisposable
        foreach (IEffect effect in effects.Values)
        {
            if (effect is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        projectionStates.Clear();
        featureStates.Clear();
        rootActionReducers.Clear();
        effects.Clear();
        middlewares.Clear();
    }

    /// <inheritdoc />
    public TState GetFeatureState<TState>()
        where TState : class, IFeatureState
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        string featureKey = TState.FeatureKey;
        if (featureStates.TryGetValue(featureKey, out object? state))
        {
            return (TState)state;
        }

        // If no reducer registered, try to create default state
        // This allows querying unregistered feature states (returns default)
        throw new InvalidOperationException(
            $"No reducer registered for feature state '{featureKey}'. " +
            $"Call RegisterFeatureState<{typeof(TState).Name}>() before selecting.");
    }

    /// <inheritdoc />
    public IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        (Type, string) key = (typeof(T), entityId);
        if (projectionStates.TryGetValue(key, out object? state))
        {
            return (IProjectionState<T>)state;
        }

        return null;
    }

    /// <summary>
    ///     Registers an effect for handling async operations.
    /// </summary>
    /// <param name="effect">The effect instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="effect" /> is null.</exception>
    public void RegisterEffect(
        IEffect effect
    )
    {
        ArgumentNullException.ThrowIfNull(effect);
        ObjectDisposedException.ThrowIf(disposed, this);
        string key = effect.GetType().FullName ?? effect.GetType().Name;
        effects[key] = effect;
    }

    /// <summary>
    ///     Registers a feature state with its root action reducer from DI.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the store was not created with a service provider.
    /// </exception>
    public void RegisterFeatureState<TState>()
        where TState : class, IFeatureState, new()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (serviceProvider is null)
        {
            throw new InvalidOperationException(
                "Cannot register feature state without a service provider. " +
                "Use the constructor that accepts IServiceProvider.");
        }

        string featureKey = TState.FeatureKey;
        featureStates[featureKey] = new TState();
        IRootActionReducer<TState>? rootReducer = serviceProvider.GetService<IRootActionReducer<TState>>();
        if (rootReducer is not null)
        {
            rootActionReducers[featureKey] = rootReducer;
        }
    }

    /// <summary>
    ///     Registers a middleware in the dispatch pipeline.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware" /> is null.</exception>
    public void RegisterMiddleware(
        IMiddleware middleware
    )
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ObjectDisposedException.ThrowIf(disposed, this);
        middlewares.Add(middleware);
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

    private Action<IAction> BuildMiddlewarePipeline(
        Action<IAction> coreDispatch
    )
    {
        Action<IAction> next = coreDispatch;

        // Build pipeline in reverse order (last middleware wraps first)
        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            IMiddleware middleware = middlewares[i];
            Action<IAction> currentNext = next;
            next = action => middleware.Invoke(action, currentNext);
        }

        return next;
    }

    private void CoreDispatch(
        IAction action
    )
    {
        // First, run reducers for feature states
        ReduceFeatureStates(action);

        // Then, handle built-in projection actions
        ReduceProjectionAction(action);

        // Notify listeners of state change
        NotifyListeners();

        // Finally, trigger effects asynchronously
        _ = TriggerEffectsAsync(action);
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

    private void ReduceFeatureStates(
        IAction action
    )
    {
        // Use root action reducers for all feature states
        foreach (KeyValuePair<string, object> kvp in rootActionReducers)
        {
            string featureKey = kvp.Key;
            object rootReducer = kvp.Value;
            if (!featureStates.TryGetValue(featureKey, out object? currentState))
            {
                continue;
            }

            // Use reflection to call Reduce on the generic root action reducer
            Type rootReducerType = rootReducer.GetType();
            MethodInfo? reduceMethod = rootReducerType.GetMethod("Reduce");
            if (reduceMethod == null)
            {
                continue;
            }

            object? newState = reduceMethod.Invoke(rootReducer, [currentState, action]);
            if ((newState != null) && !ReferenceEquals(newState, currentState))
            {
                featureStates[featureKey] = newState;
            }
        }
    }

    private void ReduceProjectionAction(
        IAction action
    )
    {
        // Handle built-in projection actions
        Type actionType = action.GetType();
        if (!actionType.IsGenericType)
        {
            return;
        }

        Type genericDef = actionType.GetGenericTypeDefinition();
        Type projectionType = actionType.GetGenericArguments()[0];
        if (genericDef == typeof(ProjectionLoadingAction<>))
        {
            ReduceProjectionLoading(projectionType, (IRippleAction)action);
        }
        else if (genericDef == typeof(ProjectionLoadedAction<>))
        {
            ReduceProjectionLoaded(projectionType, action);
        }
        else if (genericDef == typeof(ProjectionUpdatedAction<>))
        {
            ReduceProjectionUpdated(projectionType, action);
        }
        else if (genericDef == typeof(ProjectionErrorAction<>))
        {
            ReduceProjectionError(projectionType, (IRippleAction)action, action);
        }
        else if (genericDef == typeof(ProjectionConnectionChangedAction<>))
        {
            ReduceProjectionConnectionChanged(projectionType, (IRippleAction)action, action);
        }
    }

    private void ReduceProjectionConnectionChanged(
        Type projectionType,
        IRippleAction rippleAction,
        IAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo isConnectedProp = actionType.GetProperty("IsConnected")!;
        bool isConnected = (bool)isConnectedProp.GetValue(action)!;
        (Type, string) key = (projectionType, rippleAction.EntityId);
        if (projectionStates.TryGetValue(key, out object? existingState))
        {
            Type stateType = existingState.GetType();
            object withIsConnected = SetStateProperty(stateType, existingState, "IsConnected", isConnected);
            projectionStates[key] = withIsConnected;
        }
    }

    private void ReduceProjectionError(
        Type projectionType,
        IRippleAction rippleAction,
        IAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo errorProp = actionType.GetProperty("Error")!;
        Exception error = (Exception)errorProp.GetValue(action)!;
        (Type, string) key = (projectionType, rippleAction.EntityId);

        // Preserve existing data if available
        object? existingData = null;
        long? existingVersion = null;
        if (projectionStates.TryGetValue(key, out object? existingState))
        {
            Type stateType = existingState.GetType();
            existingData = stateType.GetProperty("Data")?.GetValue(existingState);
            existingVersion = (long?)stateType.GetProperty("Version")?.GetValue(existingState);
        }

        Type newStateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        ConstructorInfo constructor = newStateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([rippleAction.EntityId]);
        newState = SetStateProperty(newStateType, newState, "Data", existingData);
        newState = SetStateProperty(newStateType, newState, "Version", existingVersion);
        newState = SetStateProperty(newStateType, newState, "IsLoading", false);
        newState = SetStateProperty(newStateType, newState, "IsLoaded", false);
        newState = SetStateProperty(newStateType, newState, "LastError", error);
        projectionStates[key] = newState;
    }

    private void ReduceProjectionLoaded(
        Type projectionType,
        IAction action
    )
    {
        Type actionType = action.GetType();
        PropertyInfo entityIdProp = actionType.GetProperty("EntityId")!;
        PropertyInfo dataProp = actionType.GetProperty("Data")!;
        PropertyInfo versionProp = actionType.GetProperty("Version")!;
        string entityId = (string)entityIdProp.GetValue(action)!;
        object? data = dataProp.GetValue(action);
        long version = (long)versionProp.GetValue(action)!;
        (Type, string) key = (projectionType, entityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        ConstructorInfo constructor = stateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([entityId]);
        newState = SetStateProperty(stateType, newState, "Data", data);
        newState = SetStateProperty(stateType, newState, "Version", version);
        newState = SetStateProperty(stateType, newState, "IsLoaded", true);
        newState = SetStateProperty(stateType, newState, "IsLoading", false);
        projectionStates[key] = newState;
    }

    private void ReduceProjectionLoading(
        Type projectionType,
        IRippleAction action
    )
    {
        (Type, string) key = (projectionType, action.EntityId);
        Type stateType = typeof(ProjectionState<>).MakeGenericType(projectionType);
        ConstructorInfo constructor = stateType.GetConstructor([typeof(string)])!;
        object newState = constructor.Invoke([action.EntityId]);
        newState = SetStateProperty(stateType, newState, "IsLoading", true);
        projectionStates[key] = newState;
    }

    private void ReduceProjectionUpdated(
        Type projectionType,
        IAction action
    )
    {
        // Same logic as Loaded - updates data and version
        ReduceProjectionLoaded(projectionType, action);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; store must remain stable")]
    private async Task TriggerEffectsAsync(
        IAction action
    )
    {
        foreach (IEffect effect in effects.Values)
        {
            if (!effect.CanHandle(action))
            {
                continue;
            }

            try
            {
                await foreach (IAction resultAction in effect.HandleAsync(action, CancellationToken.None))
                {
                    Dispatch(resultAction);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when effect is cancelled; don't propagate
            }
            catch (Exception)
            {
                // Effects should handle their own errors by emitting error actions.
                // Swallow exceptions here to prevent effect failures from breaking dispatch.
                // In production, effects should catch their own exceptions and emit error actions.
            }
        }
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