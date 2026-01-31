using System;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SelectorExtensions" />.
/// </summary>
public sealed class SelectorExtensionsTests : IDisposable
{
    private readonly TestStore store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectorExtensionsTests" /> class.
    /// </summary>
    public SelectorExtensionsTests()
    {
        store = new();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
    }

    /// <summary>
    ///     Minimal test store for unit testing selector extensions.
    /// </summary>
    private sealed class TestStore : IStore
    {
        private OtherFeatureState otherState = new();

        private TestFeatureState testState = new();

        private ThirdFeatureState thirdState = new();

        public void Dispatch(
            IAction action
        )
        {
            if (action is TestAction testAction)
            {
                testState = testState with
                {
                    Counter = testAction.NewValue,
                };
            }
            else if (action is OtherAction otherAction)
            {
                otherState = otherState with
                {
                    Name = otherAction.NewName,
                };
            }
            else if (action is ThirdAction thirdAction)
            {
                thirdState = thirdState with
                {
                    IsActive = thirdAction.NewValue,
                };
            }
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        public TState GetState<TState>()
            where TState : class, IFeatureState
        {
            if (typeof(TState) == typeof(TestFeatureState))
            {
                return (TState)(object)testState;
            }

            if (typeof(TState) == typeof(OtherFeatureState))
            {
                return (TState)(object)otherState;
            }

            if (typeof(TState) == typeof(ThirdFeatureState))
            {
                return (TState)(object)thirdState;
            }

            throw new InvalidOperationException($"Unknown state type: {typeof(TState)}");
        }

        public IDisposable Subscribe(
            Action listener
        ) =>
            new NoOpDisposable();

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
                // No-op
            }
        }
    }

    /// <summary>
    ///     Test action for modifying TestFeatureState.
    /// </summary>
    private sealed record TestAction(int NewValue) : IAction;

    /// <summary>
    ///     Test action for modifying OtherFeatureState.
    /// </summary>
    private sealed record OtherAction(string NewName) : IAction;

    /// <summary>
    ///     Test action for modifying ThirdFeatureState.
    /// </summary>
    private sealed record ThirdAction(bool NewValue) : IAction;

    /// <summary>
    ///     Test feature state.
    /// </summary>
    private sealed record TestFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test-feature";

        /// <summary>
        ///     Gets the counter value.
        /// </summary>
        public int Counter { get; init; }
    }

    /// <summary>
    ///     Other feature state for multi-state selector tests.
    /// </summary>
    private sealed record OtherFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "other-feature";

        /// <summary>
        ///     Gets the name value.
        /// </summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    ///     Third feature state for three-state selector tests.
    /// </summary>
    private sealed record ThirdFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "third-feature";

        /// <summary>
        ///     Gets a value indicating whether this state is active.
        /// </summary>
        public bool IsActive { get; init; }
    }

    /// <summary>
    ///     Select with single state returns derived value.
    /// </summary>
    [Fact]
    public void SelectSingleStateReturnsDerivedValue()
    {
        // Arrange
        store.Dispatch(new TestAction(42));

        // Act
        int result = store.Select<TestFeatureState, int>(state => state.Counter * 2);

        // Assert
        Assert.Equal(84, result);
    }

    /// <summary>
    ///     Select with single state uses current state.
    /// </summary>
    [Fact]
    public void SelectSingleStateUsesCurrentState()
    {
        // Arrange
        store.Dispatch(new TestAction(10));

        // Act
        int firstResult = store.Select<TestFeatureState, int>(state => state.Counter);

        store.Dispatch(new TestAction(20));

        int secondResult = store.Select<TestFeatureState, int>(state => state.Counter);

        // Assert
        Assert.Equal(10, firstResult);
        Assert.Equal(20, secondResult);
    }

    /// <summary>
    ///     Select with two states returns combined derived value.
    /// </summary>
    [Fact]
    public void SelectTwoStatesReturnsCombinedDerivedValue()
    {
        // Arrange
        store.Dispatch(new TestAction(5));
        store.Dispatch(new OtherAction("test"));

        // Act
        string result = store.Select<TestFeatureState, OtherFeatureState, string>(
            (test, other) => $"{other.Name}:{test.Counter}");

        // Assert
        Assert.Equal("test:5", result);
    }

    /// <summary>
    ///     Select with null store throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectWithNullStoreThrowsArgumentNullException()
    {
        // Arrange
        IStore? nullStore = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            nullStore!.Select<TestFeatureState, int>(state => state.Counter));
    }

    /// <summary>
    ///     Select with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            store.Select<TestFeatureState, int>(null!));
    }

    /// <summary>
    ///     Select two states with null store throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectTwoStatesWithNullStoreThrowsArgumentNullException()
    {
        // Arrange
        IStore? nullStore = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            nullStore!.Select<TestFeatureState, OtherFeatureState, string>((s1, s2) => string.Empty));
    }

    /// <summary>
    ///     Select two states with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectTwoStatesWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            store.Select<TestFeatureState, OtherFeatureState, string>(null!));
    }

    /// <summary>
    ///     Select three states with null store throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectThreeStatesWithNullStoreThrowsArgumentNullException()
    {
        // Arrange
        IStore? nullStore = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            nullStore!.Select<TestFeatureState, OtherFeatureState, ThirdFeatureState, string>(
                (s1, s2, s3) => string.Empty));
    }

    /// <summary>
    ///     Select three states with null selector throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SelectThreeStatesWithNullSelectorThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            store.Select<TestFeatureState, OtherFeatureState, ThirdFeatureState, string>(null!));
    }

    /// <summary>
    ///     Select three states returns combined derived value.
    /// </summary>
    [Fact]
    public void SelectThreeStatesReturnsCombinedDerivedValue()
    {
        // Arrange
        store.Dispatch(new TestAction(5));
        store.Dispatch(new OtherAction("test"));
        store.Dispatch(new ThirdAction(true));

        // Act
        string result = store.Select<TestFeatureState, OtherFeatureState, ThirdFeatureState, string>(
            (test, other, third) => $"{other.Name}:{test.Counter}:{third.IsActive}");

        // Assert
        Assert.Equal("test:5:True", result);
    }
}
