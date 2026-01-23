using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Refraction.Pages.Scenes;

/// <summary>
///     Base class for scene components that connect to Reservoir state.
/// </summary>
/// <typeparam name="TState">The feature state type this scene subscribes to.</typeparam>
/// <remarks>
///     <para>
///         Scenes are page-level compositions that:
///         <list type="bullet">
///             <item>Subscribe to a Reservoir feature state</item>
///             <item>Re-render when state changes</item>
///             <item>Pass state down to pure Refraction components via parameters</item>
///             <item>Dispatch actions in response to component events</item>
///         </list>
///     </para>
///     <para>
///         This base class handles the subscription lifecycle automatically.
///         Derived scenes access state via the <see cref="State" /> property
///         and dispatch actions via the <see cref="Store" /> property.
///     </para>
/// </remarks>
public abstract class SceneBase<TState>
    : ComponentBase,
      IDisposable
    where TState : class, IFeatureState
{
    private bool disposed;

    private IDisposable? storeSubscription;

    /// <summary>
    ///     Gets a value indicating whether the scene has encountered an error.
    /// </summary>
    /// <remarks>
    ///     Override this to provide custom error detection based on state properties.
    /// </remarks>
    protected virtual bool HasError => false;

    /// <summary>
    ///     Gets a value indicating whether the scene is currently loading data.
    /// </summary>
    /// <remarks>
    ///     Override this to provide custom loading detection based on state properties.
    /// </remarks>
    protected virtual bool IsLoading => false;

    /// <summary>
    ///     Gets the current feature state.
    /// </summary>
    protected TState State => Store.GetState<TState>();

    /// <summary>
    ///     Gets the injected store instance.
    /// </summary>
    [Inject]
    protected IStore Store { get; private set; } = null!;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Creates an event handler that dispatches an action when invoked.
    /// </summary>
    /// <typeparam name="TAction">The action type to dispatch.</typeparam>
    /// <param name="actionFactory">Factory function to create the action.</param>
    /// <returns>A task-returning event callback.</returns>
    protected Func<Task> DispatchOnEvent<TAction>(
        Func<TAction> actionFactory
    )
        where TAction : class
    {
        return () =>
        {
            Store.Dispatch((IAction)actionFactory());
            return Task.CompletedTask;
        };
    }

    /// <summary>
    ///     Creates an event handler that dispatches an action based on event args.
    /// </summary>
    /// <typeparam name="TEventArgs">The event argument type.</typeparam>
    /// <typeparam name="TAction">The action type to dispatch.</typeparam>
    /// <param name="actionFactory">Factory function to create the action from event args.</param>
    /// <returns>A task-returning event callback.</returns>
    protected Func<TEventArgs, Task> DispatchOnEvent<TEventArgs, TAction>(
        Func<TEventArgs, TAction> actionFactory
    )
        where TAction : class
    {
        return args =>
        {
            Store.Dispatch((IAction)actionFactory(args));
            return Task.CompletedTask;
        };
    }

    /// <summary>
    ///     Releases resources used by the scene.
    /// </summary>
    /// <param name="disposing">True if called from Dispose; false if from finalizer.</param>
    protected virtual void Dispose(
        bool disposing
    )
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            storeSubscription?.Dispose();
        }

        disposed = true;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        storeSubscription?.Dispose();
        storeSubscription = Store.Subscribe(StateHasChanged);
    }
}