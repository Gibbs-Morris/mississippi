using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="ContinueSagaCommandHandler{TSaga}" />.
/// </summary>
public sealed class ContinueSagaCommandHandlerTests
{
    /// <summary>
    ///     Verifies handler fails when saga has not started.
    /// </summary>
    [Fact]
    public void HandleFailsWhenSagaNotStarted()
    {
        ContinueSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ContinueSagaCommand command = new()
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "corr-1",
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies handler emits resume-requested event for started saga.
    /// </summary>
    [Fact]
    public void HandleReturnsSagaResumeRequested()
    {
        DateTimeOffset now = new(2025, 2, 20, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        ContinueSagaCommandHandler<TestSagaState> handler = new(timeProvider);
        ContinueSagaCommand command = new()
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "corr-2",
        };
        TestSagaState state = new()
        {
            SagaId = command.SagaId,
            Phase = SagaPhase.Running,
            LastCompletedStepIndex = 0,
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.True(result.Success);
        SagaResumeRequested resumeRequested = Assert.IsType<SagaResumeRequested>(Assert.Single(result.Value));
        Assert.Equal(command.SagaId, resumeRequested.SagaId);
        Assert.Equal(command.CorrelationId, resumeRequested.CorrelationId);
        Assert.Equal(now, resumeRequested.RequestedAt);
    }
    /// <summary>
    ///     Verifies failed saga recovery is rejected without emitting a resume event.
    /// </summary>
    [Fact]
    public void HandleRejectsFailedSagaWithoutRecoveryDirection()
    {
        ContinueSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            Phase = SagaPhase.Failed,
            LastCompletedStepIndex = 0,
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new ContinueSagaCommand
        {
            SagaId = state.SagaId,
        }, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Null(result.Value);
    }
    /// <summary>
    ///     Verifies compensation is rejected without emitting a resume event when its cursor is unavailable.
    /// </summary>
    [Fact]
    public void HandleRejectsCompensatingSagaWithoutDurableCursor()
    {
        ContinueSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        TestSagaState state = new()
        {
            SagaId = Guid.NewGuid(),
            Phase = SagaPhase.Compensating,
            LastCompletedStepIndex = 1,
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new ContinueSagaCommand
        {
            SagaId = state.SagaId,
        }, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Null(result.Value);
    }
}