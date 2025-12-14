using Mississippi.EventSourcing.Reducers.Abstractions;

using Xunit;


namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Tests for IReducer interface contract and behavior.
/// </summary>
#pragma warning disable CA1707 // Test methods use underscore naming convention per repository standards
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public sealed class IReducerTests
{
    [Fact]
    public void Reduce_Should_ReturnNewModel_GivenValidInputs()
    {
        // Arrange
        var reducer = new TestReducer();
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = reducer.Reduce(model, @event);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Reduce_Should_NotMutateOriginalModel_GivenValidInputs()
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

    private sealed record TestModel
    {
        public int Value { get; init; }
    }

    private sealed record TestEvent
    {
        public int Delta { get; init; }
    }

    private sealed class TestReducer : IReducer<TestModel, TestEvent>
    {
        public TestModel Reduce(
            TestModel model,
            TestEvent @event
        )
        {
            return model with { Value = model.Value + @event.Delta };
        }
    }
}
