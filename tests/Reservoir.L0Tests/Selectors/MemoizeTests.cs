using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Selectors;


namespace Mississippi.Reservoir.L0Tests.Selectors;

/// <summary>
///     Tests for <see cref="Memoize" />.
/// </summary>
public sealed class MemoizeTests
{
    /// <summary>
    ///     Second test state for multi-state tests.
    /// </summary>
    private sealed record OtherState(string Name);

    /// <summary>
    ///     Test state for unit tests.
    /// </summary>
    private sealed record TestState(int Counter);

    /// <summary>
    ///     Third test state for three-state tests.
    /// </summary>
    private sealed record ThirdState(bool IsActive);

    /// <summary>
    ///     Create three-state with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void CreateThreeStateWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Memoize.Create<TestState, OtherState, ThirdState, string>(null!));
    }

    /// <summary>
    ///     Create two-state with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void CreateTwoStateWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Memoize.Create<TestState, OtherState, string>(null!));
    }

    /// <summary>
    ///     Create with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void CreateWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Memoize.Create<TestState, int>(null!));
    }

    /// <summary>
    ///     Memoized selector recomputes when state reference changes.
    /// </summary>
    [Fact]
    public void MemoizedSelectorRecomputesWhenStateChanges()
    {
        // Arrange
        TestState state1 = new(10);
        TestState state2 = new(20);
        int callCount = 0;
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s =>
        {
            callCount++;
            return s.Counter * 2;
        });

        // Act
        int result1 = selector(state1);
        int result2 = selector(state2);

        // Assert
        Assert.Equal(20, result1);
        Assert.Equal(40, result2);
        Assert.Equal(2, callCount);
    }

    /// <summary>
    ///     Memoized selector recomputes when state changes back to original value.
    /// </summary>
    [Fact]
    public void MemoizedSelectorRecomputesWhenStateChangesBackToOriginal()
    {
        // Arrange
        TestState state1 = new(10);
        TestState state2 = new(20);
        TestState state3 = new(10); // Same value as state1, but different reference
        int callCount = 0;
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s =>
        {
            callCount++;
            return s.Counter * 2;
        });

        // Act
        _ = selector(state1);
        _ = selector(state2);
        _ = selector(state3);

        // Assert - should have 3 calls because all are different references
        Assert.Equal(3, callCount);
    }

    /// <summary>
    ///     Memoized selector returns cached value when state reference unchanged.
    /// </summary>
    [Fact]
    public void MemoizedSelectorReturnsCachedValueWhenStateUnchanged()
    {
        // Arrange
        TestState state = new(42);
        int callCount = 0;
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s =>
        {
            callCount++;
            return s.Counter * 2;
        });

        // Act
        _ = selector(state);
        _ = selector(state);
        _ = selector(state);

        // Assert
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Memoized selector returns computed value on first call.
    /// </summary>
    [Fact]
    public void MemoizedSelectorReturnsComputedValueOnFirstCall()
    {
        // Arrange
        TestState state = new(42);
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s => s.Counter * 2);

        // Act
        int result = selector(state);

        // Assert
        Assert.Equal(84, result);
    }

    /// <summary>
    ///     Three-state memoized selector caches when all states unchanged.
    /// </summary>
    [Fact]
    public void ThreeStateMemoizedSelectorCachesWhenAllStatesUnchanged()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        ThirdState state3 = new(true);
        int callCount = 0;
        Func<TestState, OtherState, ThirdState, string> selector =
            Memoize.Create<TestState, OtherState, ThirdState, string>((
                s1,
                s2,
                s3
            ) =>
            {
                callCount++;
                return $"{s2.Name}:{s1.Counter}:{s3.IsActive}";
            });

        // Act
        _ = selector(state1, state2, state3);
        _ = selector(state1, state2, state3);
        _ = selector(state1, state2, state3);

        // Assert
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Three-state memoized selector recomputes when any state changes.
    /// </summary>
    [Fact]
    public void ThreeStateMemoizedSelectorRecomputesWhenAnyStateChanges()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        ThirdState state3A = new(true);
        ThirdState state3B = new(false);
        int callCount = 0;
        Func<TestState, OtherState, ThirdState, string> selector =
            Memoize.Create<TestState, OtherState, ThirdState, string>((
                s1,
                s2,
                s3
            ) =>
            {
                callCount++;
                return $"{s2.Name}:{s1.Counter}:{s3.IsActive}";
            });

        // Act
        _ = selector(state1, state2, state3A);
        _ = selector(state1, state2, state3B);

        // Assert
        Assert.Equal(2, callCount);
    }

    /// <summary>
    ///     Two-state memoized selector caches when both states unchanged.
    /// </summary>
    [Fact]
    public void TwoStateMemoizedSelectorCachesWhenBothStatesUnchanged()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        int callCount = 0;
        Func<TestState, OtherState, string> selector = Memoize.Create<TestState, OtherState, string>((
            s1,
            s2
        ) =>
        {
            callCount++;
            return $"{s2.Name}:{s1.Counter}";
        });

        // Act
        _ = selector(state1, state2);
        _ = selector(state1, state2);
        _ = selector(state1, state2);

        // Assert
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Two-state memoized selector recomputes when first state changes.
    /// </summary>
    [Fact]
    public void TwoStateMemoizedSelectorRecomputesWhenFirstStateChanges()
    {
        // Arrange
        TestState state1A = new(5);
        TestState state1B = new(10);
        OtherState state2 = new("test");
        int callCount = 0;
        Func<TestState, OtherState, string> selector = Memoize.Create<TestState, OtherState, string>((
            s1,
            s2
        ) =>
        {
            callCount++;
            return $"{s2.Name}:{s1.Counter}";
        });

        // Act
        _ = selector(state1A, state2);
        _ = selector(state1B, state2);

        // Assert
        Assert.Equal(2, callCount);
    }

    /// <summary>
    ///     Two-state memoized selector recomputes when second state changes.
    /// </summary>
    [Fact]
    public void TwoStateMemoizedSelectorRecomputesWhenSecondStateChanges()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2A = new("alpha");
        OtherState state2B = new("beta");
        int callCount = 0;
        Func<TestState, OtherState, string> selector = Memoize.Create<TestState, OtherState, string>((
            s1,
            s2
        ) =>
        {
            callCount++;
            return $"{s2.Name}:{s1.Counter}";
        });

        // Act
        _ = selector(state1, state2A);
        _ = selector(state1, state2B);

        // Assert
        Assert.Equal(2, callCount);
    }

    /// <summary>
    ///     Two-state memoized selector returns computed value.
    /// </summary>
    [Fact]
    public void TwoStateMemoizedSelectorReturnsComputedValue()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        Func<TestState, OtherState, string> selector = Memoize.Create<TestState, OtherState, string>((
                s1,
                s2
            ) => $"{s2.Name}:{s1.Counter}");

        // Act
        string result = selector(state1, state2);

        // Assert
        Assert.Equal("test:5", result);
    }

    /// <summary>
    ///     Concurrent access with same state reference returns cached value and invokes selector once.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentAccessWithSameStateReturnsCachedValueAsync()
    {
        // Arrange
        TestState state = new(42);
        int callCount = 0;
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s =>
        {
            Interlocked.Increment(ref callCount);
            return s.Counter * 2;
        });

        const int threadCount = 100;
        Task<int>[] tasks = new Task<int>[threadCount];

        // Act - launch many concurrent calls with the same state reference
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() => selector(state));
        }

        int[] results = await Task.WhenAll(tasks);

        // Assert - all results should be correct
        Assert.All(results, result => Assert.Equal(84, result));

        // Selector should be called exactly once (first call computes, rest use cache)
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Concurrent access with different state references handles contention correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentAccessWithDifferentStatesHandlesContentionAsync()
    {
        // Arrange
        TestState[] states = Enumerable.Range(0, 10).Select(i => new TestState(i)).ToArray();
        int callCount = 0;
        Func<TestState, int> selector = Memoize.Create<TestState, int>(s =>
        {
            Interlocked.Increment(ref callCount);
            return s.Counter * 2;
        });

        const int iterationsPerState = 50;
        List<Task<int>> tasks = [];

        // Act - launch many concurrent calls with different state references
        foreach (TestState state in states)
        {
            for (int i = 0; i < iterationsPerState; i++)
            {
                TestState capturedState = state;
                tasks.Add(Task.Run(() => selector(capturedState)));
            }
        }

        int[] results = await Task.WhenAll(tasks);

        // Assert - all results should be valid (even values between 0 and 18)
        Assert.All(results, result =>
        {
            Assert.True(result >= 0 && result <= 18);
            Assert.Equal(0, result % 2);
        });

        // Call count should be reasonable (at least 1, could be more due to cache invalidation)
        Assert.True(callCount >= 1);
    }

    /// <summary>
    ///     Two-state memoized selector handles concurrent access correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TwoStateConcurrentAccessHandlesContentionAsync()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        int callCount = 0;
        Func<TestState, OtherState, string> selector = Memoize.Create<TestState, OtherState, string>((
            s1,
            s2
        ) =>
        {
            Interlocked.Increment(ref callCount);
            return $"{s2.Name}:{s1.Counter}";
        });

        const int threadCount = 100;
        Task<string>[] tasks = new Task<string>[threadCount];

        // Act - launch many concurrent calls with the same state references
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() => selector(state1, state2));
        }

        string[] results = await Task.WhenAll(tasks);

        // Assert - all results should be correct
        Assert.All(results, result => Assert.Equal("test:5", result));

        // Selector should be called exactly once
        Assert.Equal(1, callCount);
    }

    /// <summary>
    ///     Three-state memoized selector handles concurrent access correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ThreeStateConcurrentAccessHandlesContentionAsync()
    {
        // Arrange
        TestState state1 = new(5);
        OtherState state2 = new("test");
        ThirdState state3 = new(true);
        int callCount = 0;
        Func<TestState, OtherState, ThirdState, string> selector =
            Memoize.Create<TestState, OtherState, ThirdState, string>((
                s1,
                s2,
                s3
            ) =>
            {
                Interlocked.Increment(ref callCount);
                return $"{s2.Name}:{s1.Counter}:{s3.IsActive}";
            });

        const int threadCount = 100;
        Task<string>[] tasks = new Task<string>[threadCount];

        // Act - launch many concurrent calls with the same state references
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() => selector(state1, state2, state3));
        }

        string[] results = await Task.WhenAll(tasks);

        // Assert - all results should be correct
        Assert.All(results, result => Assert.Equal("test:5:True", result));

        // Selector should be called exactly once
        Assert.Equal(1, callCount);
    }
}