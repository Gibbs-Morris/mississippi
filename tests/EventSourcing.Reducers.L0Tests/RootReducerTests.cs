using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Validates the default root reducer behavior.
/// </summary>
public sealed class RootReducerTests
{
    private static async IAsyncEnumerable<object> CreateAsyncEventsAsync(
        params int[] events
    )
    {
        foreach (int value in events)
        {
            await Task.Yield();
            yield return value;
        }
    }

    private static NumberModel CreateModel(
        int value
    ) =>
        new(value);

    private sealed record CounterModel(int AppliedEvents, int Total);

    private sealed record IncrementEvent(int Amount);

    private sealed class MutableModel
    {
        public MutableModel(
            int value
        ) =>
            Value = value;

        public int Value { get; private set; }

        public override bool Equals(
            object? obj
        ) =>
            obj is MutableModel other && (Value == other.Value);

        public override int GetHashCode() => Value;

        public void Increment(
            int amount
        )
        {
            Value += amount;
        }
    }

    private sealed class MutatingReducer : IReducer<MutableModel>
    {
        public MutableModel Reduce(
            MutableModel input,
            object eventData
        )
        {
            if (eventData is int amount)
            {
                input.Increment(amount);
            }

            return input;
        }
    }

    private sealed class NoOpReducer : IReducer<MutableModel>
    {
        public MutableModel Reduce(
            MutableModel input,
            object eventData
        ) =>
            input;
    }

    private sealed record NumberModel(int Value);

    private sealed class RecordCounterReducer : IReducer<CounterModel>
    {
        public CounterModel Reduce(
            CounterModel input,
            object eventData
        )
        {
            if (eventData is not IncrementEvent incrementEvent)
            {
                return input;
            }

            // Records use with-expressions to produce an updated immutable copy.
            return input with
            {
                AppliedEvents = input.AppliedEvents + 1,
                Total = input.Total + incrementEvent.Amount,
            };
        }
    }

    private sealed class TestReducer : IReducer<NumberModel>
    {
        private readonly Func<int, int, int> reduce;

        public TestReducer(
            Func<int, int, int> reduce
        ) =>
            this.reduce = reduce;

        public NumberModel Reduce(
            NumberModel input,
            object eventData
        )
        {
            if (eventData is not int value)
            {
                return input;
            }

            return input with
            {
                Value = reduce(input.Value, value),
            };
        }
    }

    /// <summary>
    ///     Ensures reducers may return the same reference when no changes are required.
    /// </summary>
    [Fact]
    public void ReduceAllowsReturningSameInstanceWhenStateUnchanged()
    {
        IReducer<MutableModel>[] reducers = new IReducer<MutableModel>[] { new NoOpReducer() };
        RootReducer<MutableModel> rootReducer = new(reducers);
        MutableModel startingModel = new(5);
        MutableModel result = rootReducer.Reduce(startingModel, Array.Empty<object>());
        Assert.Same(startingModel, result);
    }

    /// <summary>
    ///     Ensures the synchronous reducer applies nested reducers sequentially.
    /// </summary>
    [Fact]
    public void ReduceAppliesReducersSequentially()
    {
        IReducer<NumberModel>[] reducers = new IReducer<NumberModel>[]
        {
            new TestReducer((
                current,
                value
            ) => current + value),
            new TestReducer((
                current,
                value
            ) => current * value),
        };
        RootReducer<NumberModel> rootReducer = new(reducers);
        NumberModel result = rootReducer.Reduce(CreateModel(1), new object[] { 2, 3 });
        Assert.Equal(CreateModel(27), result);
    }

    /// <summary>
    ///     Ensures the asynchronous reducer applies nested reducers sequentially.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous assertion.</returns>
    [Fact]
    public async Task ReduceAsyncAppliesReducersSequentially()
    {
        IReducer<NumberModel>[] reducers = new IReducer<NumberModel>[]
        {
            new TestReducer((
                current,
                value
            ) => current + value),
            new TestReducer((
                current,
                value
            ) => current - value),
        };
        RootReducer<NumberModel> rootReducer = new(reducers);
        IAsyncEnumerable<object> events = CreateAsyncEventsAsync(4, 1);
        NumberModel result = await rootReducer.ReduceAsync(CreateModel(10), events, CancellationToken.None);
        Assert.Equal(CreateModel(10), result);
    }

    /// <summary>
    ///     Ensures cancellation tokens are honored during asynchronous reduction.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous assertion.</returns>
    [Fact]
    public async Task ReduceAsyncThrowsWhenCancellationRequested()
    {
        IReducer<NumberModel>[] reducers = new IReducer<NumberModel>[]
        {
            new TestReducer((
                current,
                value
            ) => current + value),
        };
        RootReducer<NumberModel> rootReducer = new(reducers);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await rootReducer.ReduceAsync(CreateModel(0), CreateAsyncEventsAsync(1), cancellationTokenSource.Token);
        });
    }

    /// <summary>
    ///     Ensures asynchronous reductions also enforce immutability.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous assertion.</returns>
    [Fact]
    public async Task ReduceAsyncThrowsWhenReducerMutatesModelInstance()
    {
        IReducer<MutableModel>[] reducers = new IReducer<MutableModel>[] { new MutatingReducer() };
        RootReducer<MutableModel> rootReducer = new(reducers);
        MutableModel startingModel = new(0);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await rootReducer.ReduceAsync(startingModel, CreateAsyncEventsAsync(1));
        });
    }

    /// <summary>
    ///     Demonstrates the recommended record + with-expression pattern for immutable reducers.
    /// </summary>
    [Fact]
    public void ReduceSupportsRecordStatesUsingWithExpressions()
    {
        CounterModel startingModel = new(0, 0);
        object[] events = new object[] { new IncrementEvent(5), new IncrementEvent(2) };
        RootReducer<CounterModel> rootReducer = new(new IReducer<CounterModel>[] { new RecordCounterReducer() });
        CounterModel result = rootReducer.Reduce(startingModel, events);
        Assert.Equal(new(2, 7), result);
        Assert.NotSame(startingModel, result);
    }

    /// <summary>
    ///     Ensures reducers cannot mutate the existing model instance when producing changes.
    /// </summary>
    [Fact]
    public void ReduceThrowsWhenReducerMutatesModelInstance()
    {
        IReducer<MutableModel>[] reducers = new IReducer<MutableModel>[] { new MutatingReducer() };
        RootReducer<MutableModel> rootReducer = new(reducers);
        MutableModel startingModel = new(0);
        Assert.Throws<InvalidOperationException>(() => rootReducer.Reduce(startingModel, new object[] { 1 }));
    }
}
