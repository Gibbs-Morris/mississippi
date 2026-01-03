namespace Mississippi.Ripples.Blazor;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Mississippi.Ripples.Abstractions;

/// <summary>
/// Base component class that provides automatic ripple subscription lifecycle management.
/// Components inheriting from this class can use <see cref="UseRipple{T}"/> to subscribe
/// to ripples and have them automatically disposed when the component is disposed.
/// </summary>
public abstract class RippleComponent : ComponentBase, IAsyncDisposable
{
    private readonly List<IDisposable> subscriptions = [];
    private bool disposed;

    /// <summary>
    /// Subscribes to a ripple's Changed event and automatically calls StateHasChanged
    /// when the ripple updates. The subscription is automatically disposed when the
    /// component is disposed.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="ripple">The ripple to subscribe to.</param>
    protected void UseRipple<T>(IRipple<T> ripple)
        where T : class
    {
        if (ripple == null)
        {
            return;
        }

        EventHandler handler = (_, _) => _ = InvokeAsync(StateHasChanged);
        ripple.Changed += handler;

        subscriptions.Add(new RippleSubscription<T>(ripple, handler));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed resources asynchronously.
    /// Override this method in derived classes to add custom disposal logic.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        if (disposed)
        {
            return ValueTask.CompletedTask;
        }

        disposed = true;

        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }

        subscriptions.Clear();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Internal subscription tracker that removes the event handler on dispose.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    private sealed class RippleSubscription<T> : IDisposable
        where T : class
    {
        private IRipple<T> Ripple { get; }

        private EventHandler Handler { get; }

        public RippleSubscription(IRipple<T> ripple, EventHandler handler)
        {
            Ripple = ripple;
            Handler = handler;
        }

        public void Dispose()
        {
            Ripple.Changed -= Handler;
        }
    }
}
