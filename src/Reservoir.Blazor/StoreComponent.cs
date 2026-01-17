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
///         Derived components can use <see cref="GetState{TState}" /> to access feature states.
///         The component automatically re-renders when any state changes.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         @inherits StoreComponent
///
///         @code {
///             private SidebarState Sidebar => GetState&lt;SidebarState&gt;();
///
///             private void ToggleSidebar() => Dispatch(new ToggleSidebarAction());
///         }
///     </code>
/// </example>
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

    private void OnStoreChanged()
    {
        // Trigger re-render on state change - fire and forget is intentional for Blazor
        _ = InvokeAsync(StateHasChanged);
    }
}