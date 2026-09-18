using System;
using System.Collections.Generic;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Tracks removal of a listener from a test store.
/// </summary>
internal sealed class TrackingStoreSubscription : IDisposable
{
    private readonly List<Action> listeners;

    private Action? listener;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TrackingStoreSubscription" /> class.
    /// </summary>
    /// <param name="listeners">The listener collection.</param>
    /// <param name="listener">The listener to remove on disposal.</param>
    public TrackingStoreSubscription(
        List<Action> listeners,
        Action listener
    )
    {
        this.listeners = listeners;
        this.listener = listener;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (listener is not null)
        {
            listeners.Remove(listener);
            listener = null;
        }
    }
}
