using System;

using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaStepCompletedReducer{TSaga}" />.
/// </summary>
public sealed class SagaStepCompletedReducerTests
{
    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaStepCompletedReducer<TestSagaState> sut = new();
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
        SagaStepCompletedReducer<TestSagaState> sut = new();
        SagaStepCompletedEvent eventData = new("Step1", 1, DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }

    /// <summary>
    ///     Verifies Reduce updates step progress correctly.
    /// </summary>
    [Fact]
    public void ReduceShouldUpdateStepProgress()
    {
        // Arrange
        SagaStepCompletedReducer<TestSagaState> sut = new();
        TestSagaState state = new();
        SagaStepCompletedEvent eventData = new("SendNotification", 2, DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(2, result.LastCompletedStepIndex);
        Assert.Equal(1, result.CurrentStepAttempt);
    }
}