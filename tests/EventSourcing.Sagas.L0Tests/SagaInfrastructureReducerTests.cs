using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for saga infrastructure reducers.
/// </summary>
public sealed class SagaInfrastructureReducerTests
{
    /// <summary>
    ///     Verifies the saga compensated reducer updates the phase.
    /// </summary>
    [Fact]
    public void SagaCompensatedReducerSetsPhase()
    {
        SagaCompensatedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Compensating,
        };
        SagaCompensated @event = new()
        {
            CompletedAt = new(2025, 2, 11, 12, 30, 0, TimeSpan.Zero),
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(SagaPhase.Compensated, updated.Phase);
    }

    /// <summary>
    ///     Verifies the saga compensating reducer updates the phase.
    /// </summary>
    [Fact]
    public void SagaCompensatingReducerSetsPhase()
    {
        SagaCompensatingReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompensating @event = new()
        {
            FromStepIndex = 1,
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(SagaPhase.Compensating, updated.Phase);
    }

    /// <summary>
    ///     Verifies the saga completed reducer updates the phase.
    /// </summary>
    [Fact]
    public void SagaCompletedReducerSetsPhase()
    {
        SagaCompletedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompleted @event = new()
        {
            CompletedAt = new(2025, 2, 11, 12, 0, 0, TimeSpan.Zero),
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(SagaPhase.Completed, updated.Phase);
    }

    /// <summary>
    ///     Verifies the saga failed reducer updates the phase.
    /// </summary>
    [Fact]
    public void SagaFailedReducerSetsPhase()
    {
        SagaFailedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Compensating,
        };
        SagaFailed @event = new()
        {
            ErrorCode = "ERR",
            ErrorMessage = "Failure",
            FailedAt = new(2025, 2, 11, 13, 0, 0, TimeSpan.Zero),
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(SagaPhase.Failed, updated.Phase);
    }

    /// <summary>
    ///     Verifies the saga started reducer updates state.
    /// </summary>
    [Fact]
    public void SagaStartedReducerUpdatesState()
    {
        SagaStartedReducer<TestSagaState> reducer = new();
        Guid sagaId = Guid.NewGuid();
        DateTimeOffset startedAt = new(2025, 2, 11, 10, 0, 0, TimeSpan.Zero);
        TestSagaState initial = new()
        {
            Phase = SagaPhase.NotStarted,
            Name = "Alpha",
        };
        SagaStartedEvent @event = new()
        {
            SagaId = sagaId,
            StepHash = "HASH",
            StartedAt = startedAt,
            CorrelationId = "corr-1",
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.NotSame(initial, updated);
        Assert.Equal(sagaId, updated.SagaId);
        Assert.Equal(SagaPhase.Running, updated.Phase);
        Assert.Equal(-1, updated.LastCompletedStepIndex);
        Assert.Equal("corr-1", updated.CorrelationId);
        Assert.Equal(startedAt, updated.StartedAt);
        Assert.Equal("HASH", updated.StepHash);
        Assert.Equal("Alpha", updated.Name);
    }

    /// <summary>
    ///     Verifies the saga step completed reducer updates the step index.
    /// </summary>
    [Fact]
    public void SagaStepCompletedReducerUpdatesStepIndex()
    {
        SagaStepCompletedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            LastCompletedStepIndex = -1,
        };
        SagaStepCompleted @event = new()
        {
            StepIndex = 2,
            StepName = "Credit",
            CompletedAt = new(2025, 2, 11, 11, 0, 0, TimeSpan.Zero),
        };
        TestSagaState updated = reducer.Reduce(initial, @event);
        Assert.Equal(2, updated.LastCompletedStepIndex);
    }
}