using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Xunit;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for RootReducer implementation.
/// </summary>
#pragma warning disable CA1707 // Test methods use underscore naming convention per repository standards
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public sealed class RootReducerTests
{
    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_GivenNullReducers()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootReducer<TestModel>(null!));
    }

    [Fact]
    public void Reduce_Should_ThrowArgumentNullException_GivenNullEvent()
    {
        // Arrange
        var reducers = Array.Empty<IReducer<TestModel, object>>();
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rootReducer.Reduce(model, null!));
    }

    [Fact]
    public void Reduce_Should_ThrowArgumentNullException_GivenNullEvents()
    {
        // Arrange
        var reducers = Array.Empty<IReducer<TestModel, object>>();
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rootReducer.Reduce(model, (IEnumerable<object>)null!));
    }

    [Fact]
    public void Reduce_Should_ApplySingleEvent_GivenOneReducer()
    {
        // Arrange
        var reducer = new TestReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = rootReducer.Reduce(model, @event);

        // Assert
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Reduce_Should_ApplyMultipleEvents_GivenMultipleReducers()
    {
        // Arrange
        var reducer1 = new TestReducer();
        var reducer2 = new StringReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer1, reducer2 };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10, Text = "hello" };
        var event1 = new TestEvent { Delta = 5 };
        var event2 = new StringEvent { Suffix = " world" };

        // Act
        TestModel result1 = rootReducer.Reduce(model, event1);
        TestModel result2 = rootReducer.Reduce(result1, event2);

        // Assert
        Assert.Equal(15, result2.Value);
        Assert.Equal("hello world", result2.Text);
    }

    [Fact]
    public void Reduce_Should_ApplyEventSequence_GivenMultipleEvents()
    {
        // Arrange
        var reducer = new TestReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var events = new object[]
        {
            new TestEvent { Delta = 5 },
            new TestEvent { Delta = 3 },
            new TestEvent { Delta = 2 },
        };

        // Act
        TestModel result = rootReducer.Reduce(model, events);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void Reduce_Should_ReturnOriginalModel_GivenNoMatchingReducers()
    {
        // Arrange
        var reducers = Array.Empty<IReducer<TestModel, object>>();
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = rootReducer.Reduce(model, @event);

        // Assert
        Assert.Equal(10, result.Value);
        Assert.Same(model, result);
    }

    [Fact]
    public async Task ReduceAsync_Should_ApplySingleEvent_GivenOneReducer()
    {
        // Arrange
        var reducer = new TestReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = await rootReducer.ReduceAsync(model, @event);

        // Assert
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public async Task ReduceAsync_Should_ApplyEventSequence_GivenMultipleEvents()
    {
        // Arrange
        var reducer = new TestReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var events = new object[]
        {
            new TestEvent { Delta = 5 },
            new TestEvent { Delta = 3 },
            new TestEvent { Delta = 2 },
        };

        // Act
        TestModel result = await rootReducer.ReduceAsync(model, events);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task ReduceAsync_Should_RespectCancellationToken_GivenCancelledToken()
    {
        // Arrange
        var reducer = new TestReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rootReducer.ReduceAsync(model, @event, cts.Token)
        );
    }

    [Fact]
    public void Reduce_Should_NotMutateOriginalModel_GivenMultipleReducers()
    {
        // Arrange
        var reducer1 = new TestReducer();
        var reducer2 = new StringReducer();
        var reducers = new IReducer<TestModel, object>[] { reducer1, reducer2 };
        var rootReducer = new RootReducer<TestModel>(reducers);
        var model = new TestModel { Value = 10, Text = "hello" };
        var events = new object[]
        {
            new TestEvent { Delta = 5 },
            new StringEvent { Suffix = " world" },
        };

        // Act
        TestModel result = rootReducer.Reduce(model, events);

        // Assert
        Assert.Equal(10, model.Value);
        Assert.Equal("hello", model.Text);
        Assert.Equal(15, result.Value);
        Assert.Equal("hello world", result.Text);
    }

    private sealed record TestModel
    {
        public int Value { get; init; }

        public string Text { get; init; } = string.Empty;
    }

    private sealed record TestEvent
    {
        public int Delta { get; init; }
    }

    private sealed record StringEvent
    {
        public string Suffix { get; init; } = string.Empty;
    }

    private sealed class TestReducer : IReducer<TestModel, object>
    {
        public TestModel Reduce(
            TestModel model,
            object @event
        )
        {
            if (@event is TestEvent testEvent)
            {
                return model with { Value = model.Value + testEvent.Delta };
            }

            return model;
        }
    }

    private sealed class StringReducer : IReducer<TestModel, object>
    {
        public TestModel Reduce(
            TestModel model,
            object @event
        )
        {
            if (@event is StringEvent stringEvent)
            {
                return model with { Text = model.Text + stringEvent.Suffix };
            }

            return model;
        }
    }
}
