using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="RootEventEffect{TAggregate}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("RootEventEffect")]
public sealed class RootEventEffectTests
{
    /// <summary>
    ///     Fallback effect that handles via CanHandle but has no type index.
    /// </summary>
    private sealed class FallbackEffect : IEventEffect<TestAggregate>
    {
        public bool WasInvoked { get; private set; }

        /// <inheritdoc />
        public bool CanHandle(
            object eventData
        ) =>
            eventData is SecondEvent;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        /// <inheritdoc />
        public async IAsyncEnumerable<object> HandleAsync(
            object eventData,
            TestAggregate currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            WasInvoked = true;
            yield break;
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Second test event for matching.
    /// </summary>
    private sealed record SecondEvent;

    /// <summary>
    ///     Test aggregate state.
    /// </summary>
    private sealed record TestAggregate(int Count);

    /// <summary>
    ///     Test event for matching.
    /// </summary>
    private sealed record TestEvent(string Value);

    /// <summary>
    ///     Effect that throws an exception.
    /// </summary>
    private sealed class ThrowingEffect : EventEffectBase<TestEvent, TestAggregate>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<object> HandleAsync(
            TestEvent eventData,
            TestAggregate currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    /// <summary>
    ///     Simple effect that tracks invocation.
    /// </summary>
    private sealed class TrackingSimpleEffect : SimpleEventEffectBase<TestEvent, TestAggregate>
    {
        public bool WasInvoked { get; private set; }

        /// <inheritdoc />
        protected override Task HandleSimpleAsync(
            TestEvent eventData,
            TestAggregate currentState,
            CancellationToken cancellationToken
        )
        {
            WasInvoked = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Unhandled event type.
    /// </summary>
    private sealed record UnhandledEvent;

    /// <summary>
    ///     Effect that yields events.
    /// </summary>
    private sealed class YieldingEffect : EventEffectBase<TestEvent, TestAggregate>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<object> HandleAsync(
            TestEvent eventData,
            TestAggregate currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            yield return new SecondEvent();
        }
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when effects is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorWithNullEffectsThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootEventEffect<TestAggregate>(null!));
    }

    /// <summary>
    ///     DispatchAsync continues to other effects when one throws an exception.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Error Handling")]
    public async Task DispatchAsyncContinuesToOtherEffectsWhenOneThrows()
    {
        // Arrange
        TrackingSimpleEffect trackingEffect = new();
        RootEventEffect<TestAggregate> sut = new([new ThrowingEffect(), trackingEffect]);
        TestEvent eventData = new("test");
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert - throwing effect should not prevent subsequent effects from running
        Assert.True(trackingEffect.WasInvoked);
    }

    /// <summary>
    ///     DispatchAsync dispatches to fallback effects for non-indexed types.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task DispatchAsyncDispatchesToFallbackEffects()
    {
        // Arrange
        FallbackEffect fallbackEffect = new();
        RootEventEffect<TestAggregate> sut = new([fallbackEffect]);
        SecondEvent eventData = new();
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.True(fallbackEffect.WasInvoked);
    }

    /// <summary>
    ///     DispatchAsync dispatches to matching effect and yields events.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task DispatchAsyncDispatchesToMatchingEffectAndYieldsEvents()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([new YieldingEffect()]);
        TestEvent eventData = new("test");
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<SecondEvent>(results[0]);
    }

    /// <summary>
    ///     DispatchAsync invokes multiple effects for same event type.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task DispatchAsyncInvokesMultipleEffectsForSameEventType()
    {
        // Arrange
        TrackingSimpleEffect effect1 = new();
        TrackingSimpleEffect effect2 = new();
        RootEventEffect<TestAggregate> sut = new([effect1, effect2]);
        TestEvent eventData = new("test");
        TestAggregate state = new(1);

        // Act
        await foreach (object item in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            _ = item; // Consume the enumerable
        }

        // Assert
        Assert.True(effect1.WasInvoked);
        Assert.True(effect2.WasInvoked);
    }

    /// <summary>
    ///     DispatchAsync returns empty when no effects match.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task DispatchAsyncReturnsEmptyWhenNoEffectsMatch()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([new YieldingEffect()]);
        UnhandledEvent eventData = new();
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    ///     DispatchAsync throws ArgumentNullException when eventData is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void DispatchAsyncThrowsArgumentNullExceptionWhenEventDataIsNull()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([]);
        TestAggregate state = new(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.DispatchAsync(null!, state, CancellationToken.None));
    }

    /// <summary>
    ///     DispatchAsync with logger does not throw.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Logging")]
    public async Task DispatchAsyncWithLoggerDoesNotThrow()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([new YieldingEffect()]);
        TestEvent eventData = new("test");
        TestAggregate state = new(1);

        // Act
        List<object> results = [];
        await foreach (object result in sut.DispatchAsync(eventData, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Single(results);
    }

    /// <summary>
    ///     EffectCount returns correct count when effects are registered.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void EffectCountReturnsCorrectCountWhenEffectsRegistered()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([new YieldingEffect(), new TrackingSimpleEffect()]);

        // Assert
        Assert.Equal(2, sut.EffectCount);
    }

    /// <summary>
    ///     EffectCount returns zero when no effects are registered.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void EffectCountReturnsZeroWhenNoEffectsRegistered()
    {
        // Arrange
        RootEventEffect<TestAggregate> sut = new([]);

        // Assert
        Assert.Equal(0, sut.EffectCount);
    }
}