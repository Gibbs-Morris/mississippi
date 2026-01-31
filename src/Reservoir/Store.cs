using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Central state container implementing Redux-like dispatch pattern.
///     Supports local feature states with actions, action reducers, middleware, and action effects.
/// </summary>
public class Store : IStore
{
    private readonly ConcurrentDictionary<string, object> featureStates = new();

    /// <summary>
    ///     Cache of MethodInfo for HandleAsync methods, keyed by root effect type.
    ///     Avoids repeated reflection lookups on every dispatch.
    /// </summary>
    private readonly ConcurrentDictionary<Type, MethodInfo?> handleAsyncMethodCache = new();

    private readonly ConcurrentDictionary<string, object> initialFeatureStates = new();

    private readonly List<Action> listeners = [];

    private readonly object listenersLock = new();

    private readonly List<IMiddleware> middlewares = [];

    private readonly ConcurrentDictionary<string, object> rootActionEffects = new();

    private readonly ConcurrentDictionary<string, object> rootReducers = new();

    private readonly StoreEventSubject<StoreEventBase> storeEventSubject = new();

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
    /// <param name="middlewaresCollection">The middlewares to register in the dispatch pipeline.</param>
    /// <remarks>
    ///     All effects are feature-scoped via <see cref="IActionEffect{TState}" /> and are
    ///     resolved through feature state registrations. There are no global effects.
    /// </remarks>
    public Store(
        IEnumerable<IFeatureStateRegistration> featureRegistrations,
        IEnumerable<IMiddleware> middlewaresCollection
    )
    {
        ArgumentNullException.ThrowIfNull(featureRegistrations);
        ArgumentNullException.ThrowIfNull(middlewaresCollection);

        // Initialize feature states from registrations
        foreach (IFeatureStateRegistration registration in featureRegistrations)
        {
            featureStates[registration.FeatureKey] = registration.InitialState;
            initialFeatureStates[registration.FeatureKey] = registration.InitialState;
            if (registration.RootReducer is not null)
            {
                rootReducers[registration.FeatureKey] = registration.RootReducer;
            }

            if (registration.RootActionEffect is not null)
            {
                rootActionEffects[registration.FeatureKey] = registration.RootActionEffect;
            }
        }

        // Register middleware
        foreach (IMiddleware middleware in middlewaresCollection)
        {
            RegisterMiddleware(middleware);
        }

        // Emit initialization event
        storeEventSubject.OnNext(new StoreInitializedEvent(GetStateSnapshot()));
    }

    /// <inheritdoc />
    public IObservable<StoreEventBase> StoreEvents => storeEventSubject;

    /// <inheritdoc />
    public void Dispatch(
        IAction action
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(action);

        // Handle system actions directly without going through user reducers/effects
        if (action is ISystemAction systemAction)
        {
            HandleSystemAction(systemAction);
            return;
        }

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

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetStateSnapshot() =>
        new Dictionary<string, object>(featureStates);

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
            storeEventSubject.Dispose();
            lock (listenersLock)
            {
                listeners.Clear();
            }

            featureStates.Clear();
            rootReducers.Clear();
            rootActionEffects.Clear();
            handleAsyncMethodCache.Clear();
            middlewares.Clear();
            initialFeatureStates.Clear();
        }
    }

    /// <summary>
    ///     Gets a snapshot of the initial feature states keyed by feature key.
    /// </summary>
    /// <returns>A snapshot dictionary of initial feature states.</returns>
    [SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Returns a new dictionary instance each call; method semantics are correct")]
    protected IReadOnlyDictionary<string, object> GetInitialStateSnapshot() =>
        new Dictionary<string, object>(initialFeatureStates);

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
        // Emit pre-dispatch event
        storeEventSubject.OnNext(new ActionDispatchingEvent(action));

        // Run reducers for feature states
        ReduceFeatureStates(action);

        // Emit post-dispatch event with current state snapshot
        storeEventSubject.OnNext(new ActionDispatchedEvent(action, GetStateSnapshot()));

        // Notify listeners of state change
        NotifyListeners();

        // Finally, trigger action effects asynchronously
        _ = TriggerEffectsAsync(action);
    }

    private void HandleSystemAction(
        ISystemAction systemAction
    )
    {
        // Emit pre-dispatch event for system actions too
        storeEventSubject.OnNext(new ActionDispatchingEvent(systemAction));

        IReadOnlyDictionary<string, object> previousSnapshot = GetStateSnapshot();
        IReadOnlyDictionary<string, object> newSnapshot;
        bool notify;

        switch (systemAction)
        {
            case RestoreStateAction restoreAction:
                ApplyStateSnapshot(restoreAction.Snapshot);
                newSnapshot = GetStateSnapshot();
                notify = restoreAction.NotifyListeners;
                break;

            case ResetToInitialStateAction resetAction:
                ApplyStateSnapshot(GetInitialStateSnapshot());
                newSnapshot = GetStateSnapshot();
                notify = resetAction.NotifyListeners;
                break;

            default:
                // Unknown system action - just emit the dispatching event
                return;
        }

        // Emit state restored event
        storeEventSubject.OnNext(new StateRestoredEvent(previousSnapshot, newSnapshot, systemAction));

        if (notify)
        {
            NotifyListeners();
        }
    }

    private void ApplyStateSnapshot(
        IReadOnlyDictionary<string, object> newStates
    )
    {
        foreach (KeyValuePair<string, object> kvp in newStates)
        {
            if (!featureStates.TryGetValue(kvp.Key, out object? currentState))
            {
                continue;
            }

            object? newState = kvp.Value;
            if (newState is null)
            {
                continue;
            }

            Type currentType = currentState.GetType();
            if (!currentType.IsInstanceOfType(newState))
            {
                continue;
            }

            featureStates[kvp.Key] = newState;
        }
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
        // Dispatch to each feature's root action effect
        foreach (KeyValuePair<string, object> kvp in rootActionEffects)
        {
            string featureKey = kvp.Key;
            object rootEffect = kvp.Value;
            if (!featureStates.TryGetValue(featureKey, out object? currentState))
            {
                continue;
            }

            // Use cached reflection to call HandleAsync on the generic root action effect.
            // The MethodInfo is cached per root effect type to avoid repeated reflection lookups.
            Type rootEffectType = rootEffect.GetType();
            MethodInfo? handleMethod = handleAsyncMethodCache.GetOrAdd(
                rootEffectType,
                static type => type.GetMethod("HandleAsync"));
            if (handleMethod == null)
            {
                continue;
            }

            try
            {
                // Client-side action effects are intentionally non-cancellable once started.
                // We pass CancellationToken.None to HandleAsync to indicate that the effect
                // should not depend on external cancellation and must handle its own lifetime.
                // The currentState is passed for pattern alignment with the IActionEffect<TState>
                // interface but effects should extract all needed data from the action itself.
                object? result = handleMethod.Invoke(rootEffect, [action, currentState, CancellationToken.None]);
                if (result is not IAsyncEnumerable<IAction> asyncEnumerable)
                {
                    continue;
                }

                await foreach (IAction resultAction in asyncEnumerable)
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