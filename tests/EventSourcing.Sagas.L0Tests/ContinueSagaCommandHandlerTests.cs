using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

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
            Phase = SagaPhase.Failed,
            LastCompletedStepIndex = 0,
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaResumeRequested resumeRequested = Assert.IsType<SagaResumeRequested>(Assert.Single(result.Value));
        Assert.Equal(command.SagaId, resumeRequested.SagaId);
        Assert.Equal(command.CorrelationId, resumeRequested.CorrelationId);
        Assert.Equal(now, resumeRequested.RequestedAt);
    }
}