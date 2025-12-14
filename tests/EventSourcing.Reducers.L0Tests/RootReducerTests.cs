using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for <see cref="RootReducer{TProjection}" />.
/// </summary>
public sealed class RootReducerTests
{
    private sealed class MatchingReducer : IReducer<TestProjection>
    {
        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            if (eventData is not TestEvent)
            {
                projection = default!;
                return false;
            }

            projection = new($"{state.Value}-p1");
            return true;
        }
    }

    private sealed class CountingReducer : IReducer<TestProjection>
    {
        public int InvocationCount { get; private set; }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            InvocationCount++;
            if (eventData is not TestEvent typedEvent)
            {
                projection = default!;
                return false;
            }

            projection = new($"{state.Value}-{typedEvent.Value}-c{InvocationCount}");
            return true;
        }
    }

    private sealed record MutableEvent(string Value);

    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MutatingReducer : IReducer<MutableEvent, MutableProjection>
    {
        public MutableProjection Reduce(
            MutableProjection state,
            MutableEvent eventData
        )
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(eventData);
            state.Value = eventData.Value;
            return state;
        }

        public bool TryReduce(
            MutableProjection state,
            object eventData,
            out MutableProjection projection
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            if (eventData is not MutableEvent typedEvent)
            {
                projection = default!;
                return false;
            }

            projection = Reduce(state, typedEvent);
            return true;
        }
    }

    private sealed class NonMatchingReducer : IReducer<TestProjection>
    {
        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            projection = default!;
            return false;
        }
    }

    private sealed record TestEvent(string Value);

    private sealed record TestProjection(string Value);

    /// <summary>
    ///     Ensures reducer hash generation is stable and insensitive to reducer order.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void GetReducerHashShouldBeStableAndOrderIndependent()
    {
        IReducer<TestProjection>[] reducersA = { new MatchingReducer(), new NonMatchingReducer() };
        IReducer<TestProjection>[] reducersB = { new NonMatchingReducer(), new MatchingReducer() };
        RootReducer<TestProjection> rootA = new(reducersA);
        RootReducer<TestProjection> rootB = new(reducersB);
        string hashA = rootA.GetReducerHash();
        string hashB = rootB.GetReducerHash();
        Assert.Equal(hashA, hashB);
        Assert.NotEmpty(hashA);
    }

    /// <summary>
    ///     Ensures Reduce throws when event data is null.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        RootReducer<TestProjection> root = new(Array.Empty<IReducer<TestProjection>>());
        Assert.Throws<ArgumentNullException>(() => root.Reduce(new TestProjection("s0"), null!));
    }

    /// <summary>
    ///     Ensures reducers cannot mutate and return the same projection instance.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldRejectMutatingReducersReturningSameInstance()
    {
        IReducer<MutableProjection>[] reducers = new IReducer<MutableProjection>[] { new MutatingReducer() };
        RootReducer<MutableProjection> root = new(reducers);
        MutableProjection state = new()
        {
            Value = "s0",
        };
        Assert.Throws<InvalidOperationException>(() => root.Reduce(state, new MutableEvent("e0")));
    }

    /// <summary>
    ///     Ensures the first matching reducer determines the resulting projection.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldReturnProjectionFromFirstMatchingReducer()
    {
        IReducer<TestProjection>[] reducers = new IReducer<TestProjection>[]
        {
            new NonMatchingReducer(), new MatchingReducer(),
        };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e0"));
        Assert.Equal("s0-p1", result.Value);
    }

    /// <summary>
    ///     Ensures Reduce stops iterating after the first matching reducer.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldStopAfterFirstMatchingReducer()
    {
        CountingReducer first = new();
        ThrowingReducer second = new();
        IReducer<TestProjection>[] reducers = new IReducer<TestProjection>[] { first, second };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e1"));
        Assert.Equal("s0-e1-c1", result.Value);
        Assert.Equal(1, first.InvocationCount);
        Assert.False(second.Invoked);
    }

    /// <summary>
    ///     Ensures Reduce returns the existing state when no reducer matches the event.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldReturnStateWhenNoReducerMatchesEvent()
    {
        IReducer<TestProjection>[] reducers = Array.Empty<IReducer<TestProjection>>();
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e0"));
        Assert.Same(state, result);
    }

    private sealed class ThrowingReducer : IReducer<TestProjection>
    {
        public bool Invoked { get; private set; }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            Invoked = true;
            throw new InvalidOperationException("This reducer should not be invoked when a prior reducer matches.");
        }
    }
}