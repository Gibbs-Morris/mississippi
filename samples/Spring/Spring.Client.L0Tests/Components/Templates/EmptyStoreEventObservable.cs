using System;

using Mississippi.Reservoir.Abstractions.Events;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Represents an empty store-event stream for layout tests.
/// </summary>
internal sealed class EmptyStoreEventObservable : IObservable<StoreEventBase>
{
    /// <summary>Gets the shared empty observable.</summary>
    public static EmptyStoreEventObservable Instance { get; } = new();

    /// <summary>Subscribes an observer to the empty stream.</summary>
    /// <param name="observer">The observer, which receives no events.</param>
    /// <returns>A disposable empty subscription.</returns>
    public IDisposable Subscribe(
        IObserver<StoreEventBase> observer
    ) =>
        new EmptyStoreEventSubscription();
}