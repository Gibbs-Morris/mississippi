using System;

using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaStepRetryReducer{TSaga}" />.
/// </summary>
public sealed class SagaStepRetryReducerTests
{
    /// <summary>
    ///     Verifies Reduce updates attempt number correctly.
    /// </summary>
    [Fact]
    public void ReduceShouldUpdateAttemptNumber()
    {
        // Arrange
        SagaStepRetryReducer<TestSagaState> sut = new();
        TestSagaState state = new() { LastCompletedStepIndex = 1, CurrentStepAttempt = 1 };
        SagaStepRetryEvent eventData = new(
            "ProcessPayment",
            2,
            AttemptNumber: 3,
            MaxAttempts: 5,
            "TIMEOUT",
            "Connection timed out",
            DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(3, result.CurrentStepAttempt);
        Assert.Equal(1, result.LastCompletedStepIndex);
    }

    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaStepRetryReducer<TestSagaState> sut = new();
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
        SagaStepRetryReducer<TestSagaState> sut = new();
        SagaStepRetryEvent eventData = new(
            "Step1",
            1,
            AttemptNumber: 2,
            MaxAttempts: 3,
            "ERROR",
            "Test error",
            DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }
}
