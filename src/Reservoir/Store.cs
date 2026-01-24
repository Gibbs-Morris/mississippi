using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Central state container implementing Redux-like dispatch pattern.
///     Supports local feature states with actions, action reducers, middleware, and action effects.
/// </summary>
public class Store : IStore
{
    private readonly ConcurrentDictionary<string, IActionEffect> actionEffects = new();

    private readonly ConcurrentDictionary<string, object> featureStates = new();

    private readonly List<Action> listeners = [];

    private readonly object listenersLock = new();

    private readonly List<IMiddleware> middlewares = [];

    private readonly ConcurrentDictionary<string, object> rootReducers = new();

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Store" /> class.
    /// </summary>
    public Store()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Store" /> class with DI-resolved components.
    /// </summary>
    /// <param name="featureRegistrations">The feature state registrations to initialize.</param>
    /// <param name="effectsCollection">The action effects to register for handling async operations.</param>
    /// <param name="middlewaresCollection">The middlewares to register in the dispatch pipeline.</param>
    public Store(
        IEnumerable<IFeatureStateRegistration> featureRegistrations,
        IEnumerable<IActionEffect> effectsCollection,
        IEnumerable<IMiddleware> middlewaresCollection
    )
    {
        ArgumentNullException.ThrowIfNull(featureRegistrations);
        ArgumentNullException.ThrowIfNull(effectsCollection);
        ArgumentNullException.ThrowIfNull(middlewaresCollection);

        // Initialize feature states from registrations
        foreach (IFeatureStateRegistration registration in featureRegistrations)
        {
            featureStates[registration.FeatureKey] = registration.InitialState;
            if (registration.RootReducer is not null)
            {
                rootReducers[registration.FeatureKey] = registration.RootReducer;
            }
        }

        // Register action effects
        foreach (IActionEffect actionEffect in effectsCollection)
        {
            RegisterActionEffect(actionEffect);
        }

        // Register middleware
        foreach (IMiddleware middleware in middlewaresCollection)
        {
            RegisterMiddleware(middleware);
        }
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public TState GetState<TState>()
        where TState : class, IFeatureState
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        string featureKey = TState.FeatureKey;
        if (featureStates.TryGetValue(featureKey, out object? state))
        {
            return (TState)state;
        }

        throw new InvalidOperationException(
            $"No feature state registered for '{featureKey}'. " +
            $"Call AddFeatureState<{typeof(TState).Name}>() during service registration.");
    }

    /// <summary>
    ///     Registers an action effect for handling async operations.
    /// </summary>
    /// <param name="actionEffect">The action effect instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actionEffect" /> is null.</exception>
    public void RegisterActionEffect(
        IActionEffect actionEffect
    )
    {
        ArgumentNullException.ThrowIfNull(actionEffect);
        ObjectDisposedException.ThrowIf(disposed, this);
        string key = actionEffect.GetType().FullName ?? actionEffect.GetType().Name;
        actionEffects[key] = actionEffect;
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

    /// <summary>
    ///     Disposes resources used by the store.
    /// </summary>
    /// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
    protected virtual void Dispose(
        bool disposing
    )
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (disposing)
        {
            lock (listenersLock)
            {
                listeners.Clear();
            }

            // Dispose any action effects that implement IDisposable
            foreach (IActionEffect actionEffect in actionEffects.Values)
            {
                if (actionEffect is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            featureStates.Clear();
            rootReducers.Clear();
            actionEffects.Clear();
            middlewares.Clear();
        }
    }

    /// <summary>
    ///     Hook for derived classes to process actions after action reducers run.
    ///     Called before action effects are triggered.
    /// </summary>
    /// <param name="action">The action being dispatched.</param>
    protected virtual void OnActionDispatched(
        IAction action
    )
    {
        // Base implementation does nothing; derived classes can override
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

        // Hook for derived classes
        OnActionDispatched(action);

        // Notify listeners of state change
        NotifyListeners();

        // Finally, trigger action effects asynchronously
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
        // Use root reducers for all feature states
        foreach (KeyValuePair<string, object> kvp in rootReducers)
        {
            string featureKey = kvp.Key;
            object rootReducer = kvp.Value;
            if (!featureStates.TryGetValue(featureKey, out object? currentState))
            {
                continue;
            }

            // Use reflection to call Reduce on the generic root reducer
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

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; store must remain stable")]
    private async Task TriggerEffectsAsync(
        IAction action
    )
    {
        foreach (IActionEffect actionEffect in actionEffects.Values)
        {
            if (!actionEffect.CanHandle(action))
            {
                continue;
            }

            try
            {
                await foreach (IAction resultAction in actionEffect.HandleAsync(action, CancellationToken.None))
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
                // Action effects should handle their own errors by emitting error actions.
                // Swallow exceptions here to prevent effect failures from breaking dispatch.
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

        private Store? store;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="store">The store to subscribe to.</param>
        /// <param name="listener">The listener callback to invoke on state changes.</param>
        public Subscription(
            Store store,
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