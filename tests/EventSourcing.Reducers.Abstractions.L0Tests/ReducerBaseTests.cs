using Mississippi.EventSourcing.Reducers.Abstractions;

using Xunit;


namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Tests for ReducerBase abstract class behavior.
/// </summary>
public sealed class ReducerBaseTests
{
    [Fact]
    public void Reduce_Should_ApplyEvent_GivenMatchingEventType()
    {
        // Arrange
        var reducer = new TestReducer();
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = reducer.Reduce(model, @event);

        // Assert
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Reduce_Should_ReturnOriginalModel_GivenNonMatchingEventType()
    {
        // Arrange
        var reducer = new TestReducer();
        var model = new TestModel { Value = 10 };
        var @event = new OtherEvent { Name = "test" };

        // Act
        TestModel result = reducer.Reduce(model, @event);

        // Assert
        Assert.Equal(10, result.Value);
        Assert.Same(model, result);
    }

    [Fact]
    public void Reduce_Should_NotMutateOriginalModel_GivenMatchingEventType()
    {
        // Arrange
        var reducer = new TestReducer();
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = reducer.Reduce(model, @event);

        // Assert
        Assert.Equal(10, model.Value);
        Assert.Equal(15, result.Value);
        Assert.NotSame(model, result);
    }

    [Fact]
    public void Reduce_Should_HandleMultipleEventTypes_GivenDifferentReducers()
    {
        // Arrange
        var reducer1 = new TestReducer();
        var reducer2 = new StringReducer();
        var model = new TestModel { Value = 10, Text = "hello" };
        var event1 = new TestEvent { Delta = 5 };
        var event2 = new StringEvent { Suffix = " world" };

        // Act
        TestModel result1 = reducer1.Reduce(model, event1);
        TestModel result2 = reducer2.Reduce(result1, event2);

        // Assert
        Assert.Equal(15, result2.Value);
        Assert.Equal("hello world", result2.Text);
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

    private sealed record OtherEvent
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed record StringEvent
    {
        public string Suffix { get; init; } = string.Empty;
    }

    private sealed class TestReducer : ReducerBase<TestModel, TestEvent>
    {
        protected override TestModel Apply(
            TestModel model,
            TestEvent @event
        )
        {
            return model with { Value = model.Value + @event.Delta };
        }
    }

    private sealed class StringReducer : ReducerBase<TestModel, StringEvent>
    {
        protected override TestModel Apply(
            TestModel model,
            StringEvent @event
        )
        {
            return model with { Text = model.Text + @event.Suffix };
        }
    }
}
