using System;
using System.Collections.Generic;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Implementation of <see cref="IInletStore" /> that wraps an <see cref="IStore" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a unified interface for components that need
///         Redux-style state management. Projection state is accessed via
///         <c>GetState&lt;ProjectionsFeatureState&gt;()</c>.
///     </para>
/// </remarks>
public sealed class CompositeInletStore : IInletStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeInletStore" /> class.
    /// </summary>
    /// <param name="store">The underlying Redux-style store.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> is null.
    /// </exception>
    public CompositeInletStore(
        IStore store
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        Store = store;
    }

    /// <inheritdoc />
    public IObservable<StoreEventBase> StoreEvents => Store.StoreEvents;

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
    public TState GetState<TState>()
        where TState : class, IFeatureState =>
        Store.GetState<TState>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetStateSnapshot() => Store.GetStateSnapshot();

    /// <inheritdoc />
    public IDisposable Subscribe(
        Action listener
    ) =>
        Store.Subscribe(listener);
}