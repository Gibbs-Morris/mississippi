using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="SimpleEventEffectBase{TEvent,TAggregate}" />.
/// </summary>
public sealed class SimpleEventEffectBaseTests
{
    /// <summary>
    ///     Different event that should not match.
    /// </summary>
    private sealed record OtherEvent;

    /// <summary>
    ///     Test aggregate state.
    /// </summary>
    private sealed record TestAggregate(int Count);

    /// <summary>
    ///     Test event for matching.
    /// </summary>
    private sealed record TestEvent(string Value);

    /// <summary>
    ///     Concrete test effect implementation.
    /// </summary>
    private sealed class TestSimpleEffect : SimpleEventEffectBase<TestEvent, TestAggregate>
    {
        public TestEvent? HandledEvent { get; private set; }

        public bool WasHandled { get; private set; }

        /// <inheritdoc />
        protected override Task HandleSimpleAsync(
            TestEvent eventData,
            TestAggregate currentState,
            string brookKey,
            long eventPosition,
            CancellationToken cancellationToken
        )
        {
            WasHandled = true;
            HandledEvent = eventData;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     CanHandle returns false for non-matching event type.
    /// </summary>
    [Fact]
    public void CanHandleReturnsFalseForNonMatchingEventType()
    {
        // Arrange
        TestSimpleEffect sut = new();
        OtherEvent eventData = new();

        // Act
        bool result = sut.CanHandle(eventData);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     CanHandle returns true for matching event type (inherited from EventEffectBase).
    /// </summary>
    [Fact]
    public void CanHandleReturnsTrueForMatchingEventType()
    {
        // Arrange
        TestSimpleEffect sut = new();
        TestEvent eventData = new("test");

        // Act
        bool result = sut.CanHandle(eventData);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     HandleAsync invokes simple handler and yields no events.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncInvokesSimpleHandlerAndYieldsNoEvents()
    {
        // Arrange
        TestSimpleEffect sut = new();
        TestEvent eventData = new("test-value");
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.HandleAsync(eventData, state, "test-brook", 1L, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
        Assert.True(sut.WasHandled);
        Assert.Equal(eventData, sut.HandledEvent);
    }

    /// <summary>
    ///     HandleAsync throws ArgumentNullException when event is null.
    /// </summary>
    [Fact]
    public void HandleAsyncThrowsArgumentNullExceptionWhenEventIsNull()
    {
        // Arrange
        TestSimpleEffect sut = new();
        TestAggregate state = new(1);

        // Act & Assert
        // Note: The exception is thrown synchronously on method call, not during async enumeration
        IAsyncEnumerable<object> CallHandleAsync() =>
            sut.HandleAsync(null!, state, "test-brook", 1L, CancellationToken.None);

        Assert.Throws<ArgumentNullException>(CallHandleAsync);
    }
}