using System;
using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Deterministic test double for <see cref="IStore" />.
/// </summary>
internal sealed class TestReservoirStore : IStore
{
    private readonly Queue<TestStoreSubscription> subscriptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestReservoirStore" /> class.
    /// </summary>
    /// <param name="state">The feature state returned by <see cref="GetState{TState}" />.</param>
    /// <param name="subscriptions">The subscriptions returned in subscribe order.</param>
    public TestReservoirStore(
        TestReservoirFeatureState state,
        params TestStoreSubscription[] subscriptions
    )
    {
        CurrentState = state;
        this.subscriptions = new Queue<TestStoreSubscription>(subscriptions);
    }

    /// <summary>
    ///     Gets the actions dispatched during the test.
    /// </summary>
    public List<IAction> DispatchedActions { get; } = [];

    /// <summary>
    ///     Gets the current feature state.
    /// </summary>
    public TestReservoirFeatureState CurrentState { get; }

    /// <summary>
    ///     Gets the number of times <see cref="Subscribe" /> was called.
    /// </summary>
    public int SubscribeCallCount { get; private set; }

    /// <inheritdoc />
    public IObservable<StoreEventBase> StoreEvents { get; } = new NoOpStoreEventObservable();

    /// <inheritdoc />
    public void Dispatch(
        IAction action
    )
    {
        DispatchedActions.Add(action);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public TState GetState<TState>()
        where TState : class, IFeatureState =>
        CurrentState as TState ?? throw new InvalidOperationException($"Unsupported state type: {typeof(TState).Name}");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetStateSnapshot() =>
        new Dictionary<string, object>
        {
            [TestReservoirFeatureState.FeatureKey] = CurrentState,
        };

    /// <inheritdoc />
    public IDisposable Subscribe(
        Action listener
    )
    {
        SubscribeCallCount++;
        return subscriptions.Count > 0 ? subscriptions.Dequeue() : new TestStoreSubscription();
    }

    private sealed class NoOpStoreEventObservable : IObservable<StoreEventBase>
    {
        public IDisposable Subscribe(
            IObserver<StoreEventBase> observer
        ) =>
            new TestStoreSubscription();
    }
}
