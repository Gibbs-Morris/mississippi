using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="EventEffectBase{TEvent,TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("EventEffectBase")]
public sealed class EventEffectBaseTests
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
    ///     Concrete test effect implementation.
    /// </summary>
    private sealed class TestEffect : EventEffectBase<TestEvent, TestAggregate>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<object> HandleAsync(
            TestEvent eventData,
            TestAggregate currentState,
            string brookKey,
            long eventPosition,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            yield return new OtherEvent();
        }
    }

    /// <summary>
    ///     Test event for matching.
    /// </summary>
    private sealed record TestEvent(string Value);

    /// <summary>
    ///     CanHandle returns false for non-matching event type.
    /// </summary>
    [Fact]
    [AllureFeature("Type Checking")]
    public void CanHandleReturnsFalseForNonMatchingEventType()
    {
        // Arrange
        TestEffect sut = new();
        OtherEvent eventData = new();

        // Act
        bool result = sut.CanHandle(eventData);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     CanHandle returns true for matching event type.
    /// </summary>
    [Fact]
    [AllureFeature("Type Checking")]
    public void CanHandleReturnsTrueForMatchingEventType()
    {
        // Arrange
        TestEffect sut = new();
        TestEvent eventData = new("test");

        // Act
        bool result = sut.CanHandle(eventData);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     CanHandle throws ArgumentNullException when event is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void CanHandleThrowsArgumentNullExceptionWhenEventIsNull()
    {
        // Arrange
        TestEffect sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.CanHandle(null!));
    }

    /// <summary>
    ///     HandleAsync dispatches to typed method for matching event.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncDispatchesToTypedMethodForMatchingEvent()
    {
        // Arrange
        TestEffect sut = new();
        TestEvent eventData = new("test");
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.HandleAsync(eventData, state, "test-brook", 1L, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<OtherEvent>(results[0]);
    }

    /// <summary>
    ///     HandleAsync returns empty enumerable for non-matching event.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncReturnsEmptyForNonMatchingEvent()
    {
        // Arrange
        TestEffect sut = new();
        OtherEvent eventData = new();
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.HandleAsync(eventData, state, "test-brook", 1L, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    ///     HandleAsync throws ArgumentNullException when event is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void HandleAsyncThrowsArgumentNullExceptionWhenEventIsNull()
    {
        // Arrange
        TestEffect sut = new();
        IEventEffect<TestAggregate> effect = sut;
        TestAggregate state = new(1);

        // Act & Assert
        // Note: The exception is thrown synchronously on method call, not during async enumeration
        IAsyncEnumerable<object> CallHandleAsync() =>
            effect.HandleAsync(null!, state, "test-brook", 1L, CancellationToken.None);

        Assert.Throws<ArgumentNullException>(CallHandleAsync);
    }
}