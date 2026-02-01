using System;

using Microsoft.AspNetCore.Components;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Base component that integrates with the Reservoir state management system.
///     Automatically subscribes to store changes and disposes subscriptions on component disposal.
/// </summary>
/// <remarks>
///     <para>
///         Derived components can use <see cref="GetState{TState}" /> to access feature states
///         and <see cref="Select{TState,TResult}" /> to derive computed values.
///         The component automatically re-renders when any state changes.
///     </para>
/// </remarks>
public abstract class StoreComponent
    : ComponentBase,
      IDisposable
{
    private bool disposed;

    private IDisposable? storeSubscription;

    /// <summary>
    ///     Gets or sets the store injected from DI.
    /// </summary>
    [Inject]
    protected IStore Store { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Dispatches an action to the store.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    protected void Dispatch(
        IAction action
    )
    {
        Store.Dispatch(action);
    }

    /// <summary>
    ///     Disposes resources used by the component.
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
            // Unsubscribe from store
            storeSubscription?.Dispose();
            storeSubscription = null;
        }
    }

    /// <summary>
    ///     Gets the current state for a feature slice.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The current feature state.</returns>
    protected TState GetState<TState>()
        where TState : class, IFeatureState =>
        Store.GetState<TState>();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Dispose any existing subscription (shouldn't happen, but satisfies analyzer)
        storeSubscription?.Dispose();

        // Subscribe to store changes to trigger re-render
        storeSubscription = Store.Subscribe(OnStoreChanged);
    }

    /// <summary>
    ///     Selects a derived value from a feature state using a selector function.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     A pure function that derives a value from the feature state.
    ///     MUST be side-effect free and return the same output for the same input.
    /// </param>
    /// <returns>The derived value.</returns>
    protected TResult Select<TState, TResult>(
        Func<TState, TResult> selector
    )
        where TState : class, IFeatureState =>
        Store.Select(selector);

    /// <summary>
    ///     Selects a derived value by combining two feature states.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     A pure function that derives a value from both feature states.
    ///     MUST be side-effect free and return the same output for the same inputs.
    /// </param>
    /// <returns>The derived value.</returns>
    protected TResult Select<TState1, TState2, TResult>(
        Func<TState1, TState2, TResult> selector
    )
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState =>
        Store.Select(selector);

    /// <summary>
    ///     Selects a derived value by combining three feature states.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TState3">The third feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     A pure function that derives a value from all three feature states.
    ///     MUST be side-effect free and return the same output for the same inputs.
    /// </param>
    /// <returns>The derived value.</returns>
    protected TResult Select<TState1, TState2, TState3, TResult>(
        Func<TState1, TState2, TState3, TResult> selector
    )
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState
        where TState3 : class, IFeatureState =>
        Store.Select(selector);

    private void OnStoreChanged()
    {
        // Trigger re-render on state change - fire and forget is intentional for Blazor
        _ = InvokeAsync(StateHasChanged);
    }
}