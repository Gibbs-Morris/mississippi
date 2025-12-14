using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for <see cref="DelegateReducer{TEvent, TProjection}" />.
/// </summary>
public sealed class DelegateReducerTests
{
    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Verifies null delegates are rejected.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenDelegateIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new DelegateReducer<int, string>(null!));
    }

    /// <summary>
    ///     Verifies reducers must return a new projection instance instead of mutating and returning the same reference.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldRejectMutatingSameInstance()
    {
        DelegateReducer<string, MutableProjection> reducer = new((
            state,
            @event
        ) =>
        {
            state.Value = @event;
            return state;
        });
        MutableProjection state = new()
        {
            Value = "s0",
        };
        Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, "v1"));
    }

    /// <summary>
    ///     Verifies Reduce throws when the event is null.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        DelegateReducer<string, string> reducer = new((
                state,
                @event
            ) => state + @event);
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce("s0", null!));
    }

    /// <summary>
    ///     Verifies TryReduce enforces the immutability guard when the delegate mutates and reuses the same instance.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TryReduceShouldRejectMutatingSameInstance()
    {
        DelegateReducer<string, MutableProjection> reducer = new((
            state,
            @event
        ) =>
        {
            state.Value = @event;
            return state;
        });
        MutableProjection state = new()
        {
            Value = "s0",
        };
        Assert.Throws<InvalidOperationException>(() => reducer.TryReduce(state, "v1", out _));
    }

    /// <summary>
    ///     Verifies TryReduce forwards to the delegate and returns the produced projection.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TryReduceShouldReturnProjectionFromDelegate()
    {
        DelegateReducer<int, string> reducer = new((
                state,
                @event
            ) => $"{state}-v{@event}");
        bool reduced = reducer.TryReduce("s0", 7, out string projection);
        Assert.True(reduced);
        Assert.Equal("s0-v7", projection);
    }

    /// <summary>
    ///     Verifies the adapter forwards to the provided delegate using current state.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldReturnProjectionFromDelegate()
    {
        DelegateReducer<int, string> reducer = new((
                state,
                @event
            ) => $"{state}-v{@event}");
        string result = reducer.Reduce("s0", 42);
        Assert.Equal("s0-v42", result);
    }
}