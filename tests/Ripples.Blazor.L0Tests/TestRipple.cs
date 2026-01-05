using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor.L0Tests;

/// <summary>
///     Test ripple implementation for unit tests.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "Required for Razor component parameters.")]
public sealed class TestRipple : IRipple<TestProjection>
{
    private EventHandler? changed;

    /// <inheritdoc />
    public event EventHandler? Changed
    {
        add => changed += value;
        remove => changed -= value;
    }

#pragma warning disable CS0067 // Event is never used - required by interface
    /// <inheritdoc />
    public event EventHandler<RippleErrorEventArgs>? ErrorOccurred;
#pragma warning restore CS0067

    /// <inheritdoc />
    public TestProjection? Current { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether there are any subscribers to the Changed event.
    /// </summary>
    public bool HasSubscribers => changed != null;

    /// <inheritdoc />
    public bool IsConnected => false;

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public bool IsLoading => false;

    /// <inheritdoc />
    public Exception? LastError => null;

    /// <inheritdoc />
    public long? Version => null;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public Task RefreshAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;

    /// <summary>
    ///     Sets the current data for testing.
    /// </summary>
    /// <param name="data">The test projection data.</param>
    public void SetData(
        TestProjection data
    )
    {
        Current = data;
        IsLoaded = true;
    }

    /// <inheritdoc />
    public Task SubscribeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;

    /// <summary>
    ///     Triggers the Changed event for testing.
    /// </summary>
    public void TriggerChanged()
    {
        changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;
}