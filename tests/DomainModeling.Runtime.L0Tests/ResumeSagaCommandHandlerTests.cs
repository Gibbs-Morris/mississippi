using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="ResumeSagaCommandHandler{TSaga}" />.
/// </summary>
public sealed class ResumeSagaCommandHandlerTests
{
    /// <summary>
    ///     Verifies resume commands fail when the saga has not started.
    /// </summary>
    [Fact]
    public void HandleFailsWhenSagaMissing()
    {
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ResumeSagaCommand command = new()
        {
            Disposition = SagaRecoveryPlanDisposition.NoAction,
            Source = SagaResumeSource.Reminder,
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies no-action plans emit no additional events.
    /// </summary>
    [Fact]
    public void HandleReturnsNoEventsForNoActionDisposition()
    {
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ResumeSagaCommand command = new()
        {
            Disposition = SagaRecoveryPlanDisposition.NoAction,
            Source = SagaResumeSource.Reminder,
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        Assert.Empty(result.Value);
    }

    /// <summary>
    ///     Verifies blocked plans emit a blocked event with the supplied metadata.
    /// </summary>
    [Fact]
    public void HandleReturnsBlockedEvent()
    {
        DateTimeOffset now = new(2025, 2, 20, 8, 0, 0, TimeSpan.Zero);
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider(now));
        ResumeSagaCommand command = new()
        {
            BlockedReason = "Manual approval required.",
            Direction = SagaExecutionDirection.Forward,
            Disposition = SagaRecoveryPlanDisposition.Blocked,
            Source = SagaResumeSource.Reminder,
            StepIndex = 1,
            StepName = "Credit",
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaResumeBlocked blocked = Assert.IsType<SagaResumeBlocked>(Assert.Single(result.Value));
        Assert.Equal(now, blocked.BlockedAt);
        Assert.Equal("Manual approval required.", blocked.BlockedReason);
        Assert.Equal(SagaExecutionDirection.Forward, blocked.Direction);
        Assert.Equal(SagaResumeSource.Reminder, blocked.Source);
        Assert.Equal(1, blocked.StepIndex);
        Assert.Equal("Credit", blocked.StepName);
    }

    /// <summary>
    ///     Verifies blocked plans fail validation when required metadata is missing.
    /// </summary>
    /// <param name="missingField">The required field to omit.</param>
    [Theory]
    [InlineData("direction")]
    [InlineData("step-index")]
    [InlineData("step-name")]
    [InlineData("blocked-reason")]
    public void HandleFailsBlockedEventWhenRequiredMetadataMissing(
        string missingField
    )
    {
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ResumeSagaCommand command = missingField switch
        {
            "direction" => new ResumeSagaCommand
            {
                BlockedReason = "Manual approval required.",
                Disposition = SagaRecoveryPlanDisposition.Blocked,
                Source = SagaResumeSource.Reminder,
                StepIndex = 1,
                StepName = "Credit",
            },
            "step-index" => new ResumeSagaCommand
            {
                BlockedReason = "Manual approval required.",
                Direction = SagaExecutionDirection.Forward,
                Disposition = SagaRecoveryPlanDisposition.Blocked,
                Source = SagaResumeSource.Reminder,
                StepName = "Credit",
            },
            "step-name" => new ResumeSagaCommand
            {
                BlockedReason = "Manual approval required.",
                Direction = SagaExecutionDirection.Forward,
                Disposition = SagaRecoveryPlanDisposition.Blocked,
                Source = SagaResumeSource.Reminder,
                StepIndex = 1,
            },
            "blocked-reason" => new ResumeSagaCommand
            {
                Direction = SagaExecutionDirection.Forward,
                Disposition = SagaRecoveryPlanDisposition.Blocked,
                Source = SagaResumeSource.Reminder,
                StepIndex = 1,
                StepName = "Credit",
            },
            _ => throw new InvalidOperationException($"Unknown missing field '{missingField}'."),
        };

        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies execution plans reuse provided operation identity when replaying in-flight work.
    /// </summary>
    [Fact]
    public void HandleReusesProvidedOperationIdentityForExecuteStep()
    {
        DateTimeOffset now = new(2025, 2, 20, 8, 15, 0, TimeSpan.Zero);
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider(now));
        Guid attemptId = Guid.NewGuid();
        ResumeSagaCommand command = new()
        {
            AttemptId = attemptId,
            Direction = SagaExecutionDirection.Forward,
            Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
            OperationKey = "resume-op",
            Source = SagaResumeSource.Manual,
            StepIndex = 1,
            StepName = "Credit",
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(Assert.Single(result.Value));
        Assert.Equal(attemptId, started.AttemptId);
        Assert.Equal(SagaExecutionDirection.Forward, started.Direction);
        Assert.Equal("resume-op", started.OperationKey);
        Assert.Equal(SagaResumeSource.Manual, started.Source);
        Assert.Equal(now, started.StartedAt);
        Assert.Equal(1, started.StepIndex);
        Assert.Equal("Credit", started.StepName);
    }

    /// <summary>
    ///     Verifies execution plans generate operation identity when one is not supplied.
    /// </summary>
    [Fact]
    public void HandleGeneratesOperationIdentityWhenMissing()
    {
        DateTimeOffset now = new(2025, 2, 20, 8, 20, 0, TimeSpan.Zero);
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider(now));
        Guid sagaId = Guid.NewGuid();
        ResumeSagaCommand command = new()
        {
            Direction = SagaExecutionDirection.Compensation,
            Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
            Source = SagaResumeSource.Reminder,
            StepIndex = 2,
            StepName = "UndoCredit",
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = sagaId,
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaStepExecutionStarted started = Assert.IsType<SagaStepExecutionStarted>(Assert.Single(result.Value));
        Assert.NotEqual(Guid.Empty, started.AttemptId);
        Assert.Equal(SagaStepOperationKey.Compute(sagaId, 2, SagaExecutionDirection.Compensation), started.OperationKey);
        Assert.Equal(now, started.StartedAt);
    }

    /// <summary>
    ///     Verifies execution plans fail validation when required metadata is missing.
    /// </summary>
    /// <param name="missingField">The required field to omit.</param>
    [Theory]
    [InlineData("direction")]
    [InlineData("step-index")]
    [InlineData("step-name")]
    public void HandleFailsExecutionStartedWhenRequiredMetadataMissing(
        string missingField
    )
    {
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ResumeSagaCommand command = missingField switch
        {
            "direction" => new ResumeSagaCommand
            {
                Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
                Source = SagaResumeSource.Manual,
                StepIndex = 1,
                StepName = "Credit",
            },
            "step-index" => new ResumeSagaCommand
            {
                Direction = SagaExecutionDirection.Forward,
                Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
                Source = SagaResumeSource.Manual,
                StepName = "Credit",
            },
            "step-name" => new ResumeSagaCommand
            {
                Direction = SagaExecutionDirection.Forward,
                Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
                Source = SagaResumeSource.Manual,
                StepIndex = 1,
            },
            _ => throw new InvalidOperationException($"Unknown missing field '{missingField}'."),
        };

        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies terminal completion plans emit the corresponding saga terminal event.
    /// </summary>
    [Fact]
    public void HandleReturnsCompletedEventForCompleteSagaDisposition()
    {
        DateTimeOffset now = new(2025, 2, 20, 8, 30, 0, TimeSpan.Zero);
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider(now));
        ResumeSagaCommand command = new()
        {
            Disposition = SagaRecoveryPlanDisposition.CompleteSaga,
            Source = SagaResumeSource.Manual,
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaCompleted completed = Assert.IsType<SagaCompleted>(Assert.Single(result.Value));
        Assert.Equal(now, completed.CompletedAt);
    }

    /// <summary>
    ///     Verifies compensation-complete plans emit the corresponding terminal compensation event.
    /// </summary>
    [Fact]
    public void HandleReturnsCompensatedEventForCompensateSagaDisposition()
    {
        DateTimeOffset now = new(2025, 2, 20, 8, 40, 0, TimeSpan.Zero);
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider(now));
        ResumeSagaCommand command = new()
        {
            Disposition = SagaRecoveryPlanDisposition.CompensateSaga,
            Source = SagaResumeSource.Reminder,
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.True(result.Success);
        SagaCompensated compensated = Assert.IsType<SagaCompensated>(Assert.Single(result.Value));
        Assert.Equal(now, compensated.CompletedAt);
    }

    /// <summary>
    ///     Verifies unexpected resume dispositions fail with a command error.
    /// </summary>
    [Fact]
    public void HandleFailsForUnexpectedDisposition()
    {
        ResumeSagaCommandHandler<TestSagaState> handler = new(new FakeTimeProvider());
        ResumeSagaCommand command = new()
        {
            Disposition = (SagaRecoveryPlanDisposition)999,
            Source = SagaResumeSource.Manual,
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.NewGuid(),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }
}