using System;

using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that store listener exception handling is now fixed:
///     throwing listeners no longer break the notification chain or propagate to Dispatch callers.
/// </summary>
public sealed class StoreListenerExceptionTests : IDisposable
{
    private readonly Store sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StoreListenerExceptionTests" /> class.
    /// </summary>
    public StoreListenerExceptionTests() => sut = new();

    /// <inheritdoc />
    public void Dispose() => sut.Dispose();

    /// <summary>
    ///     FIXED: A throwing listener no longer prevents subsequent listeners from being notified.
    ///     Each listener invocation is now wrapped in try-catch.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: NotifyListeners previously iterated listeners without try-catch. " +
        "A throwing listener prevented subsequent listeners from being notified. " +
        "Each listener invocation is now wrapped in try-catch.",
        FilePath = "src/Reservoir/Store.cs",
        LineNumbers = "338-350",
        Severity = "Medium",
        Category = "LogicError")]
    public void ThrowingListenerDoesNotPreventSubsequentListenerNotification()
    {
        // Arrange
        bool secondListenerNotified = false;
        using IDisposable sub1 = sut.Subscribe(
            () => throw new InvalidOperationException("listener failure"));
        using IDisposable sub2 = sut.Subscribe(() => secondListenerNotified = true);

        // Act
        Exception? caught = Record.Exception(() => sut.Dispatch(new NoOpAction()));

        // Assert – exception is caught; second listener now fires
        Assert.Null(caught);
        Assert.True(secondListenerNotified);
    }

    /// <summary>
    ///     FIXED: A listener exception no longer propagates out of Dispatch.
    ///     Listener invocations are now guarded by try-catch.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: A listener exception previously propagated out of Dispatch. " +
        "Listener invocations are now guarded by try-catch so callers " +
        "do not receive exceptions from unrelated subscribers.",
        FilePath = "src/Reservoir/Store.cs",
        LineNumbers = "338-350",
        Severity = "Medium",
        Category = "LogicError")]
    public void ThrowingListenerDoesNotCauseDispatchToThrow()
    {
        // Arrange
        using IDisposable sub = sut.Subscribe(
            () => throw new InvalidOperationException("subscriber failure"));

        // Act & Assert – Dispatch no longer propagates the listener's exception
        Exception? caught = Record.Exception(() => sut.Dispatch(new NoOpAction()));
        Assert.Null(caught);
    }

    /// <summary>
    ///     Test action with no behavior.
    /// </summary>
    private sealed record NoOpAction : IAction;
}
