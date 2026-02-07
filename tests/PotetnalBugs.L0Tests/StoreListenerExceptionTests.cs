using System;

using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that a throwing store listener breaks the notification chain
///     and propagates its exception to the <c>Dispatch</c> caller.
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
    ///     When a subscriber's listener throws during notification, all subsequent
    ///     listeners registered after it are never notified because
    ///     <c>NotifyListeners</c> iterates a snapshot in a plain foreach without
    ///     wrapping each callback invocation in a try-catch.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "NotifyListeners iterates a listener snapshot in a plain foreach without " +
        "wrapping each callback in try-catch. A throwing listener prevents all subsequent " +
        "listeners from being notified.",
        FilePath = "src/Reservoir/Store.cs",
        LineNumbers = "338-350",
        Severity = "Medium",
        Category = "LogicError")]
    public void ThrowingListenerPreventsSubsequentListenersFromNotification()
    {
        // Arrange
        bool secondListenerNotified = false;
        using IDisposable sub1 = sut.Subscribe(
            () => throw new InvalidOperationException("listener failure"));
        using IDisposable sub2 = sut.Subscribe(() => secondListenerNotified = true);

        // Act
        Exception? caught = Record.Exception(() => sut.Dispatch(new NoOpAction()));

        // Assert – first listener's exception propagates and second listener never fires
        Assert.IsType<InvalidOperationException>(caught);
        Assert.False(secondListenerNotified);
    }

    /// <summary>
    ///     When a listener throws, the exception propagates out of <c>Dispatch</c>
    ///     because <c>NotifyListeners</c> does not guard individual listener invocations.
    ///     The caller of <c>Dispatch</c> receives an exception it has no relationship to.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "A listener exception propagates out of Dispatch because NotifyListeners " +
        "does not guard individual listener invocations. The caller of Dispatch " +
        "receives an exception originating from an unrelated subscriber.",
        FilePath = "src/Reservoir/Store.cs",
        LineNumbers = "338-350",
        Severity = "Medium",
        Category = "LogicError")]
    public void ThrowingListenerCausesDispatchToThrow()
    {
        // Arrange
        using IDisposable sub = sut.Subscribe(
            () => throw new InvalidOperationException("subscriber failure"));

        // Act & Assert – Dispatch propagates the listener's exception
        Assert.Throws<InvalidOperationException>(() => sut.Dispatch(new NoOpAction()));
    }

    /// <summary>
    ///     Test action with no behavior.
    /// </summary>
    private sealed record NoOpAction : IAction;
}
