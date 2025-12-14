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