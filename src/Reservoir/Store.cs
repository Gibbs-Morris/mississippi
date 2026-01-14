using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Central state container implementing Redux-like dispatch pattern.
///     Supports local feature states with actions, action reducers, middleware, and effects.
/// </summary>
public class Store : IStore
{
    private readonly ConcurrentDictionary<string, IEffect> effects = new();

    private readonly ConcurrentDictionary<string, object> featureStates = new();

    private readonly List<Action> listeners = [];

    private readonly object listenersLock = new();

    private readonly List<IMiddleware> middlewares = [];

    private readonly ConcurrentDictionary<string, object> rootReducers = new();

    private readonly IServiceProvider? serviceProvider;

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Store" /> class.
    /// </summary>
    public Store()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Store" /> class with DI support.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The service provider for resolving root action reducers and effects.
    /// </param>
    public Store(
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
            $"No action reducer registered for feature state '{featureKey}'. " +
            $"Call RegisterState<{typeof(TState).Name}>() before selecting.");
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

    /// <summary>
    ///     Registers a feature state with its root action reducer from DI.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the store was not created with a service provider.
    /// </exception>
    public void RegisterState<TState>()
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
        IRootReducer<TState>? rootReducer = serviceProvider.GetService<IRootReducer<TState>>();
        if (rootReducer is not null)
        {
            rootReducers[featureKey] = rootReducer;
        }
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

            // Dispose any effects that implement IDisposable
            foreach (IEffect effect in effects.Values)
            {
                if (effect is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            featureStates.Clear();
            rootReducers.Clear();
            effects.Clear();
            middlewares.Clear();
        }
    }

    /// <summary>
    ///     Hook for derived classes to process actions after action reducers run.
    ///     Called before effects are triggered.
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