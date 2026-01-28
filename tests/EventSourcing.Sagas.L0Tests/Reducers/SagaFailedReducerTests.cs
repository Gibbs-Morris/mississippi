using System;

using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.L0Tests.Helpers;
using Mississippi.EventSourcing.Sagas.Reducers;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Reducers;

/// <summary>
///     Unit tests for <see cref="SagaFailedReducer{TSaga}" />.
/// </summary>
public sealed class SagaFailedReducerTests
{
    /// <summary>
    ///     Verifies Reduce transitions phase to Failed.
    /// </summary>
    [Fact]
    public void ReduceShouldTransitionPhaseToFailed()
    {
        // Arrange
        SagaFailedReducer<TestSagaState> sut = new();
        TestSagaState state = new() { Phase = SagaPhase.Running };
        SagaFailedEvent eventData = new("Payment declined", DateTimeOffset.UtcNow);

        // Act
        TestSagaState result = sut.Reduce(state, eventData);

        // Assert
        Assert.Equal(SagaPhase.Failed, result.Phase);
    }

    /// <summary>
    ///     Verifies Reduce throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceShouldThrowWhenEventIsNull()
    {
        // Arrange
        SagaFailedReducer<TestSagaState> sut = new();
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
        SagaFailedReducer<TestSagaState> sut = new();
        SagaFailedEvent eventData = new("Error", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(null!, eventData));
    }
}
