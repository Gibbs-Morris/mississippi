using System;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Tracks how many times a test subscription has been disposed.
/// </summary>
internal sealed class TestStoreSubscription : IDisposable
{
    /// <summary>
    ///     Gets the number of times <see cref="Dispose" /> was called.
    /// </summary>
    public int DisposeCallCount { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeCallCount++;
    }
}
