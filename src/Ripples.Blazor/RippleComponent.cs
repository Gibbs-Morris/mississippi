using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.Blazor;

/// <summary>
///     Base component that integrates with the Ripples state management system.
///     Automatically subscribes to store changes and disposes subscriptions on component disposal.
/// </summary>
/// <remarks>
///     <para>
///         Derived components can use <see cref="GetFeatureState{TState}" /> to access feature states
///         and <see cref="GetProjectionState{T}" /> to access projection states.
///     </para>
///     <para>
///         Call <see cref="SubscribeToProjection{T}" /> in <see cref="ComponentBase.OnInitialized" /> or
///         <see cref="ComponentBase.OnParametersSet" /> to subscribe to projections. Subscriptions are
///         automatically cleaned up when the component is disposed.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         @inherits RippleComponent
///
///         @code {
///             [Parameter]
///             public string OrderId { get; set; } = string.Empty;
///
///             private IProjectionState&lt;OrderProjection&gt;? OrderState => GetProjectionState&lt;OrderProjection&gt;(OrderId);
///
///             protected override void OnParametersSet()
///             {
///                 SubscribeToProjection&lt;OrderProjection&gt;(OrderId);
///             }
///         }
///     </code>
/// </example>
public abstract class RippleComponent
    : ComponentBase,
      IDisposable
{
    private readonly HashSet<(Type, string)> activeProjectionSubscriptions = [];

    private bool disposed;

    private IDisposable? storeSubscription;

    /// <summary>
    ///     Gets or sets the Ripples store injected from DI.
    /// </summary>
    [Inject]
    protected IRippleStore Store { get; set; } = default!;

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

            // Unsubscribe from all active projections
            foreach ((Type projectionType, string entityId) in activeProjectionSubscriptions)
            {
                // Use reflection to dispatch UnsubscribeFromProjectionAction<T>
                Type actionType = typeof(UnsubscribeFromProjectionAction<>).MakeGenericType(projectionType);
                object action = Activator.CreateInstance(actionType, entityId)!;
                Store.Dispatch((IAction)action);
            }

            activeProjectionSubscriptions.Clear();
        }
    }

    /// <summary>
    ///     Gets the current state for a feature slice.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <returns>The current feature state.</returns>
    protected TState GetFeatureState<TState>()
        where TState : class, IFeatureState =>
        Store.GetFeatureState<TState>();

    /// <summary>
    ///     Gets the current state for a projection entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The current projection state, or null if not tracked.</returns>
    protected IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class =>
        Store.GetProjectionState<T>(entityId);

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
    ///     Subscribes to a projection for a specific entity.
    ///     Dispatches a <see cref="SubscribeToProjectionAction{T}" /> and tracks the subscription
    ///     for automatic cleanup on disposal.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier to subscribe to.</param>
    /// <remarks>
    ///     Calling this method multiple times with the same parameters is safe;
    ///     only the first call will dispatch the subscribe action.
    /// </remarks>
    protected void SubscribeToProjection<T>(
        string entityId
    )
        where T : class
    {
        (Type, string) key = (typeof(T), entityId);
        if (!activeProjectionSubscriptions.Add(key))
        {
            // Already subscribed
            return;
        }

        Dispatch(new SubscribeToProjectionAction<T>(entityId));
    }

    /// <summary>
    ///     Unsubscribes from a projection for a specific entity.
    ///     Dispatches an <see cref="UnsubscribeFromProjectionAction{T}" /> and removes the tracking.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier to unsubscribe from.</param>
    protected void UnsubscribeFromProjection<T>(
        string entityId
    )
        where T : class
    {
        (Type, string) key = (typeof(T), entityId);
        if (!activeProjectionSubscriptions.Remove(key))
        {
            // Not subscribed
            return;
        }

        Dispatch(new UnsubscribeFromProjectionAction<T>(entityId));
    }

    private void OnStoreChanged()
    {
        // Trigger re-render on state change - fire and forget is intentional for Blazor
        _ = InvokeAsync(StateHasChanged);
    }
}
