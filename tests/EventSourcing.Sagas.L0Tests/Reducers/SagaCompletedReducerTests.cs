using System;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaCompletedReducer{TSaga}" />.
/// </summary>
public sealed class SagaCompletedReducerTests
{
    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaCompletedReducer<TestSagaState> sut = new();
        TestSagaState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(state, null!));
    }

    /// <summary>
    ///     Verifies Reduce throws when state is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenStateIsNull()
    {
        // Arrange
        SagaCompletedReducer<TestSagaState> sut = new();
        SagaCompletedEvent eventData = new(DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }

    /// <summary>
    ///     Verifies Reduce transitions phase to Completed.
    /// </summary>
    [Fact]
    public void ReduceShouldTransitionPhaseToCompleted()
    {
        // Arrange
        SagaCompletedReducer<TestSagaState> sut = new();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompletedEvent eventData = new(DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(SagaPhase.Completed, result.Phase);
    }
}