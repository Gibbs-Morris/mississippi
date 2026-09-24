using System;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Represents a no-op subscription for the empty store-event stream.
/// </summary>
internal sealed class EmptyStoreEventSubscription : IDisposable
{
    /// <inheritdoc />
    public void Dispose()
    {
    }
}