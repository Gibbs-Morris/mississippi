using System;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for <see cref="RootReducer{TProjection}" />.
/// </summary>
public sealed class RootReducerTests
{
    /// <summary>
    ///     A second typed event reducer for TestEvent to test first-match-wins with duplicates.
    /// </summary>
    private sealed class AlternateTestEventEventReducer : IEventReducer<TestEvent, TestProjection>
    {
        public int InvocationCount { get; private set; }

        public TestProjection Reduce(
            TestProjection state,
            TestEvent eventData
        )
        {
            InvocationCount++;
            return new($"{state.Value}-alternate-{eventData.Value}");
        }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            if (eventData is TestEvent typedEvent)
            {
                projection = Reduce(state, typedEvent);
                return true;
            }

            projection = default!;
            return false;
        }
    }

    private sealed class CountingEventReducer : IEventReducer<TestProjection>
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

    private sealed class MatchingEventReducer : IEventReducer<TestProjection>
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

    private sealed record MutableEvent(string Value);

    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MutatingEventReducer : IEventReducer<MutableEvent, MutableProjection>
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

    private sealed class NonMatchingEventReducer : IEventReducer<TestProjection>
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

    private sealed class NullReturningEventReducer : IEventReducer<string?>
    {
        public bool TryReduce(
            string? state,
            object eventData,
            out string? projection
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            projection = null;
            return true;
        }
    }

    /// <summary>
    ///     Second event type for testing type-indexed dispatch.
    /// </summary>
    private sealed record SecondEvent(string Value);

    /// <summary>
    ///     A typed event reducer for SecondEvent to test type indexing.
    /// </summary>
    private sealed class SecondEventEventReducer : IEventReducer<SecondEvent, TestProjection>
    {
        public int InvocationCount { get; private set; }

        public TestProjection Reduce(
            TestProjection state,
            SecondEvent eventData
        )
        {
            InvocationCount++;
            return new($"{state.Value}-second-{eventData.Value}");
        }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            if (eventData is SecondEvent typedEvent)
            {
                projection = Reduce(state, typedEvent);
                return true;
            }

            projection = default!;
            return false;
        }
    }

    private sealed record TestEvent(string Value);

    private sealed record TestProjection(string Value);

    /// <summary>
    ///     Third event type for testing unmatched events.
    /// </summary>
    private sealed record ThirdEvent(string Value);

    private sealed class ThrowingEventReducer : IEventReducer<TestProjection>
    {
        public bool Invoked { get; private set; }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            Invoked = true;
            throw new InvalidOperationException(
                "This event reducer should not be invoked when a prior event reducer matches.");
        }
    }

    /// <summary>
    ///     A typed event reducer for TestEvent to test type indexing.
    /// </summary>
    private sealed class TypedTestEventEventReducer : IEventReducer<TestEvent, TestProjection>
    {
        public int InvocationCount { get; private set; }

        public TestProjection Reduce(
            TestProjection state,
            TestEvent eventData
        )
        {
            InvocationCount++;
            return new($"{state.Value}-typed-{eventData.Value}");
        }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            if (eventData is TestEvent typedEvent)
            {
                projection = Reduce(state, typedEvent);
                return true;
            }

            projection = default!;
            return false;
        }
    }

    /// <summary>
    ///     Ensures event reducer hash generation is stable and insensitive to event reducer order.
    /// </summary>
    [Fact]
    public void GetReducerHashShouldBeStableAndOrderIndependent()
    {
        IEventReducer<TestProjection>[] reducersA = { new MatchingEventReducer(), new NonMatchingEventReducer() };
        IEventReducer<TestProjection>[] reducersB = { new NonMatchingEventReducer(), new MatchingEventReducer() };
        RootReducer<TestProjection> rootA = new(reducersA);
        RootReducer<TestProjection> rootB = new(reducersB);
        string hashA = rootA.GetReducerHash();
        string hashB = rootB.GetReducerHash();
        Assert.Equal(hashA, hashB);
        Assert.NotEmpty(hashA);
    }

    /// <summary>
    ///     Ensures reducers can legitimately return null when given null state without tripping the immutability guard.
    /// </summary>
    [Fact]
    public void ReduceShouldAllowNullStateAndProjection()
    {
        IEventReducer<string?>[] reducers = new IEventReducer<string?>[] { new NullReturningEventReducer() };
        RootReducer<string?> root = new(reducers);
        string? projection = root.Reduce(null!, "e0");
        Assert.Null(projection);
    }

    /// <summary>
    ///     Ensures Reduce checks indexed reducers before fallback.
    /// </summary>
    [Fact]
    public void ReduceShouldCheckIndexedReducersBeforeFallback()
    {
        TypedTestEventEventReducer typedEventReducer = new();
        CountingEventReducer fallbackEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { typedEventReducer, fallbackEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("v1"));
        Assert.Equal("s0-typed-v1", result.Value);
        Assert.Equal(1, typedEventReducer.InvocationCount);

        // Fallback should not be invoked when indexed event reducer matches
        Assert.Equal(0, fallbackEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures Reduce dispatches to the correct event reducer via type index.
    /// </summary>
    [Fact]
    public void ReduceShouldDispatchToCorrectReducerViaTypeIndex()
    {
        TypedTestEventEventReducer testEventEventReducer = new();
        SecondEventEventReducer secondEventEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { testEventEventReducer, secondEventEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new SecondEvent("v1"));
        Assert.Equal("s0-second-v1", result.Value);
        Assert.Equal(0, testEventEventReducer.InvocationCount);
        Assert.Equal(1, secondEventEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures Reduce falls through to fallback when indexed event reducer does not match.
    /// </summary>
    [Fact]
    public void ReduceShouldFallToFallbackWhenIndexedReducerDoesNotMatch()
    {
        SecondEventEventReducer secondEventEventReducer = new();
        CountingEventReducer fallbackEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { secondEventEventReducer, fallbackEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("v1"));
        Assert.Equal("s0-v1-c1", result.Value);
        Assert.Equal(0, secondEventEventReducer.InvocationCount);
        Assert.Equal(1, fallbackEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures Reduce does not invoke reducers for other event types.
    /// </summary>
    [Fact]
    public void ReduceShouldNotInvokeReducersForOtherEventTypes()
    {
        TypedTestEventEventReducer testEventEventReducer = new();
        SecondEventEventReducer secondEventEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { testEventEventReducer, secondEventEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        root.Reduce(state, new TestEvent("v1"));
        Assert.Equal(1, testEventEventReducer.InvocationCount);
        Assert.Equal(0, secondEventEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures Reduce preserves first-match-wins ordering with duplicate event type handlers.
    /// </summary>
    [Fact]
    public void ReduceShouldPreserveFirstMatchWinsOrderingWithDuplicates()
    {
        TypedTestEventEventReducer firstEventReducer = new();
        AlternateTestEventEventReducer secondEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { firstEventReducer, secondEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("v1"));
        Assert.Equal("s0-typed-v1", result.Value);
        Assert.Equal(1, firstEventReducer.InvocationCount);
        Assert.Equal(0, secondEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures reducers cannot mutate and return the same projection instance.
    /// </summary>
    [Fact]
    public void ReduceShouldRejectMutatingReducersReturningSameInstance()
    {
        IEventReducer<MutableProjection>[] reducers =
            new IEventReducer<MutableProjection>[] { new MutatingEventReducer() };
        RootReducer<MutableProjection> root = new(reducers);
        MutableProjection state = new()
        {
            Value = "s0",
        };
        Assert.Throws<InvalidOperationException>(() => root.Reduce(state, new MutableEvent("e0")));
    }

    /// <summary>
    ///     Ensures the first matching event reducer determines the resulting projection.
    /// </summary>
    [Fact]
    public void ReduceShouldReturnProjectionFromFirstMatchingReducer()
    {
        IEventReducer<TestProjection>[] reducers = new IEventReducer<TestProjection>[]
        {
            new NonMatchingEventReducer(), new MatchingEventReducer(),
        };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e0"));
        Assert.Equal("s0-p1", result.Value);
    }

    /// <summary>
    ///     Ensures Reduce returns state when indexed reducers do not match and no fallback handles.
    /// </summary>
    [Fact]
    public void ReduceShouldReturnStateWhenNoIndexedReducerMatchesAndNoFallback()
    {
        TypedTestEventEventReducer testEventEventReducer = new();
        SecondEventEventReducer secondEventEventReducer = new();
        IEventReducer<TestProjection>[] reducers =
            new IEventReducer<TestProjection>[] { testEventEventReducer, secondEventEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new ThirdEvent("v1"));
        Assert.Same(state, result);
        Assert.Equal(0, testEventEventReducer.InvocationCount);
        Assert.Equal(0, secondEventEventReducer.InvocationCount);
    }

    /// <summary>
    ///     Ensures Reduce returns the existing state when no event reducer matches the event.
    /// </summary>
    [Fact]
    public void ReduceShouldReturnStateWhenNoReducerMatchesEvent()
    {
        IEventReducer<TestProjection>[] reducers = Array.Empty<IEventReducer<TestProjection>>();
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e0"));
        Assert.Same(state, result);
    }

    /// <summary>
    ///     Ensures Reduce stops iterating after the first matching event reducer.
    /// </summary>
    [Fact]
    public void ReduceShouldStopAfterFirstMatchingReducer()
    {
        CountingEventReducer first = new();
        ThrowingEventReducer second = new();
        IEventReducer<TestProjection>[] reducers = new IEventReducer<TestProjection>[] { first, second };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("e1"));
        Assert.Equal("s0-e1-c1", result.Value);
        Assert.Equal(1, first.InvocationCount);
        Assert.False(second.Invoked);
    }

    /// <summary>
    ///     Ensures Reduce throws when event data is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        RootReducer<TestProjection> root = new(Array.Empty<IEventReducer<TestProjection>>());
        Assert.Throws<ArgumentNullException>(() => root.Reduce(new("s0"), null!));
    }

    /// <summary>
    ///     Ensures Reduce uses fallback path for reducers without generic interface.
    /// </summary>
    [Fact]
    public void ReduceShouldUseFallbackPathForNonGenericReducers()
    {
        CountingEventReducer fallbackEventReducer = new();
        IEventReducer<TestProjection>[] reducers = new IEventReducer<TestProjection>[] { fallbackEventReducer };
        RootReducer<TestProjection> root = new(reducers);
        TestProjection state = new("s0");
        TestProjection result = root.Reduce(state, new TestEvent("v1"));
        Assert.Equal("s0-v1-c1", result.Value);
        Assert.Equal(1, fallbackEventReducer.InvocationCount);
    }
}