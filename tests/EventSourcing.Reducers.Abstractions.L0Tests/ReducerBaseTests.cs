namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Verifies the shared behavior provided by <see cref="ReducerBase{TModel, TEvent}" />.
/// </summary>
public sealed class ReducerBaseTests
{
    private sealed record NumberModel(int Value);

    private sealed class TrackingReducer : ReducerBase<NumberModel, int>
    {
        public int InvocationCount { get; private set; }

        public override NumberModel Reduce(
            NumberModel input,
            int eventData
        )
        {
            InvocationCount++;
            return input with
            {
                Value = input.Value + eventData,
            };
        }
    }

    /// <summary>
    ///     Ensures the typed Reduce overload is invoked when the event type matches.
    /// </summary>
    [Fact]
    public void ReduceInvokesTypedReducerWhenEventMatches()
    {
        TrackingReducer reducer = new();
        NumberModel result = reducer.Reduce(new(1), 2);
        Assert.Equal(new(3), result);
        Assert.Equal(1, reducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures the model instance is unchanged when the event type differs.
    /// </summary>
    [Fact]
    public void ReduceReturnsOriginalModelWhenEventDoesNotMatch()
    {
        TrackingReducer reducer = new();
        NumberModel model = new(5);
        NumberModel result = reducer.Reduce(model, "ignored");
        Assert.Same(model, result);
        Assert.Equal(0, reducer.InvocationCount);
    }
}
