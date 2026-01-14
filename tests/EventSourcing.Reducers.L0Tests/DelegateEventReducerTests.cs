using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for <see cref="DelegateEventReducer{TEvent,TProjection}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Reducers")]
[AllureSubSuite("Delegate Event Reducer")]
public sealed class DelegateEventReducerTests
{
    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Verifies null delegates are rejected.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowArgumentNullExceptionWhenDelegateIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new DelegateEventReducer<int, string>(null!));
    }

    /// <summary>
    ///     Verifies the immutability guard does not fire when both state and projection are null.
    /// </summary>
    [Fact]
    public void ReduceShouldAllowNullStateAndProjection()
    {
        DelegateEventReducer<string, string?> eventReducer = new((
            _,
            @event
        ) => @event);
        string? projection = eventReducer.Reduce(null!, "e0");
        Assert.Equal("e0", projection);
    }

    /// <summary>
    ///     Verifies reducers must return a new projection instance instead of mutating and returning the same reference.
    /// </summary>
    [Fact]
    public void ReduceShouldRejectMutatingSameInstance()
    {
        DelegateEventReducer<string, MutableProjection> eventReducer = new((
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
        Assert.Throws<InvalidOperationException>(() => eventReducer.Reduce(state, "v1"));
    }

    /// <summary>
    ///     Verifies the adapter forwards to the provided delegate using current state.
    /// </summary>
    [Fact]
    public void ReduceShouldReturnProjectionFromDelegate()
    {
        DelegateEventReducer<int, string> eventReducer = new((
                state,
                @event
            ) => $"{state}-v{@event}");
        string result = eventReducer.Reduce("s0", 42);
        Assert.Equal("s0-v42", result);
    }

    /// <summary>
    ///     Verifies Reduce throws when the event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        DelegateEventReducer<string, string> eventReducer = new((
            state,
            @event
        ) => state + @event);
        Assert.Throws<ArgumentNullException>(() => eventReducer.Reduce("s0", null!));
    }

    /// <summary>
    ///     Verifies TryReduce enforces the immutability guard when the delegate mutates and reuses the same instance.
    /// </summary>
    [Fact]
    public void TryReduceShouldRejectMutatingSameInstance()
    {
        DelegateEventReducer<string, MutableProjection> eventReducer = new((
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
        Assert.Throws<InvalidOperationException>(() => eventReducer.TryReduce(state, "v1", out MutableProjection _));
    }

    /// <summary>
    ///     Verifies TryReduce forwards to the delegate and returns the produced projection.
    /// </summary>
    [Fact]
    public void TryReduceShouldReturnProjectionFromDelegate()
    {
        DelegateEventReducer<int, string> eventReducer = new((
                state,
                @event
            ) => $"{state}-v{@event}");
        bool reduced = eventReducer.TryReduce("s0", 7, out string projection);
        Assert.True(reduced);
        Assert.Equal("s0-v7", projection);
    }
}