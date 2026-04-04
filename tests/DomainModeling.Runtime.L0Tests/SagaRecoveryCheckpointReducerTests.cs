using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for saga recovery checkpoint reducer registration and reconstruction behavior.
/// </summary>
public sealed class SagaRecoveryCheckpointReducerTests
{
    private static SagaRecoveryCheckpoint Reduce(
        params object[] events
    )
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootReducer<SagaRecoveryCheckpoint> reducer =
            provider.GetRequiredService<IRootReducer<SagaRecoveryCheckpoint>>();
        SagaRecoveryCheckpoint checkpoint = new();
        foreach (object eventData in events)
        {
            checkpoint = reducer.Reduce(checkpoint, eventData);
        }

        return checkpoint;
    }

    private sealed record TestInput(string Value);

    /// <summary>
    ///     Verifies saga orchestration registration wires the recovery checkpoint snapshot support.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersRecoveryCheckpointSnapshotSupport()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry snapshotTypeRegistry = provider.GetRequiredService<ISnapshotTypeRegistry>();
        string snapshotName = SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>();
        Assert.Equal(typeof(SagaRecoveryCheckpoint), snapshotTypeRegistry.ResolveType(snapshotName));
        Assert.Equal(snapshotName, snapshotTypeRegistry.ResolveName(typeof(SagaRecoveryCheckpoint)));
        Assert.NotNull(provider.GetRequiredService<IRootReducer<SagaRecoveryCheckpoint>>());
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ISnapshotStateConverter<SagaRecoveryCheckpoint>));
    }

    /// <summary>
    ///     Verifies forward completion clears in-flight metadata and advances the pending step index.
    /// </summary>
    [Fact]
    public void ReduceAdvancesPendingForwardStepAfterCompletion()
    {
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = new(2025, 2, 15, 11, 0, 0, TimeSpan.Zero),
            },
            new SagaStepExecutionStarted
            {
                AttemptId = Guid.NewGuid(),
                Direction = SagaExecutionDirection.Forward,
                OperationKey = "forward-key",
                Source = SagaResumeSource.Initial,
                StartedAt = new(2025, 2, 15, 11, 1, 0, TimeSpan.Zero),
                StepIndex = 0,
                StepName = "Debit",
            },
            new SagaStepCompleted
            {
                AttemptId = Guid.NewGuid(),
                StepIndex = 0,
                StepName = "Debit",
                CompletedAt = new(2025, 2, 15, 11, 2, 0, TimeSpan.Zero),
                OperationKey = "forward-key",
            });
        Assert.Equal(SagaExecutionDirection.Forward, checkpoint.PendingDirection);
        Assert.Equal(1, checkpoint.PendingStepIndex);
        Assert.Null(checkpoint.PendingStepName);
        Assert.Null(checkpoint.InFlightAttemptId);
        Assert.Null(checkpoint.InFlightOperationKey);
        Assert.Null(checkpoint.LastResumeSource);
        Assert.Equal(0, checkpoint.AutomaticAttemptCount);
    }

    /// <summary>
    ///     Verifies blocked resume events preserve the pending step and operator-visible blocked state.
    /// </summary>
    [Fact]
    public void ReduceCapturesBlockedResumeState()
    {
        DateTimeOffset startedAt = new(2025, 2, 15, 12, 30, 0, TimeSpan.Zero);
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = startedAt,
            },
            new SagaResumeBlocked
            {
                BlockedAt = startedAt.AddMinutes(3),
                BlockedReason = "Manual policy prevents automatic replay.",
                Direction = SagaExecutionDirection.Forward,
                Source = SagaResumeSource.Reminder,
                StepIndex = 1,
                StepName = "Credit",
            });
        Assert.Equal(SagaExecutionDirection.Forward, checkpoint.PendingDirection);
        Assert.Equal(1, checkpoint.PendingStepIndex);
        Assert.Equal("Credit", checkpoint.PendingStepName);
        Assert.Null(checkpoint.InFlightAttemptId);
        Assert.Null(checkpoint.InFlightOperationKey);
        Assert.Equal("Manual policy prevents automatic replay.", checkpoint.BlockedReason);
        Assert.Equal(SagaResumeSource.Reminder, checkpoint.LastResumeSource);
        Assert.Equal(startedAt.AddMinutes(3), checkpoint.LastResumeAttemptedAt);
        Assert.Equal(1, checkpoint.AutomaticAttemptCount);
        Assert.False(checkpoint.ReminderArmed);
    }

    /// <summary>
    ///     Verifies the checkpoint captures saga start metadata and reminder-driven attempt state.
    /// </summary>
    [Fact]
    public void ReduceCapturesStartAndReminderExecutionMetadata()
    {
        Guid sagaId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        DateTimeOffset startedAt = new(2025, 2, 15, 10, 0, 0, TimeSpan.Zero);
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                AccessContextFingerprint = "tenant:user-a",
                SagaId = sagaId,
                RecoveryMode = SagaRecoveryMode.ManualOnly,
                RecoveryProfile = "critical-payments",
                StepHash = "HASH",
                StartedAt = startedAt,
            },
            new SagaStepExecutionStarted
            {
                AttemptId = attemptId,
                Direction = SagaExecutionDirection.Forward,
                OperationKey = "operation-key",
                Source = SagaResumeSource.Reminder,
                StartedAt = startedAt.AddMinutes(5),
                StepIndex = 0,
                StepName = "Debit",
            });
        Assert.Equal(sagaId, checkpoint.SagaId);
        Assert.Equal("HASH", checkpoint.StepHash);
        Assert.Equal(SagaRecoveryMode.ManualOnly, checkpoint.RecoveryMode);
        Assert.Equal("critical-payments", checkpoint.RecoveryProfile);
        Assert.Equal(SagaExecutionDirection.Forward, checkpoint.PendingDirection);
        Assert.Equal(0, checkpoint.PendingStepIndex);
        Assert.Equal("Debit", checkpoint.PendingStepName);
        Assert.Equal(attemptId, checkpoint.InFlightAttemptId);
        Assert.Equal("operation-key", checkpoint.InFlightOperationKey);
        Assert.Equal(SagaResumeSource.Reminder, checkpoint.LastResumeSource);
        Assert.Equal(startedAt.AddMinutes(5), checkpoint.LastResumeAttemptedAt);
        Assert.Equal(1, checkpoint.AutomaticAttemptCount);
        Assert.Equal("tenant:user-a", checkpoint.AccessContextFingerprint);
        Assert.False(checkpoint.ReminderArmed);
    }

    /// <summary>
    ///     Verifies terminal compensated and failed events also clear pending recovery state.
    /// </summary>
    [Fact]
    public void ReduceClearsPendingStateForOtherTerminalSagaEvents()
    {
        SagaRecoveryCheckpoint compensatedCheckpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = new(2025, 2, 15, 14, 0, 0, TimeSpan.Zero),
            },
            new SagaCompensating
            {
                FromStepIndex = 0,
            },
            new SagaCompensated
            {
                CompletedAt = new(2025, 2, 15, 14, 1, 0, TimeSpan.Zero),
            });
        SagaRecoveryCheckpoint failedCheckpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = new(2025, 2, 15, 15, 0, 0, TimeSpan.Zero),
            },
            new SagaFailed
            {
                ErrorCode = "ERR",
                ErrorMessage = "failure",
                FailedAt = new(2025, 2, 15, 15, 1, 0, TimeSpan.Zero),
            });
        Assert.Null(compensatedCheckpoint.PendingDirection);
        Assert.Null(compensatedCheckpoint.PendingStepIndex);
        Assert.Null(compensatedCheckpoint.InFlightAttemptId);
        Assert.Null(failedCheckpoint.PendingDirection);
        Assert.Null(failedCheckpoint.PendingStepIndex);
        Assert.Null(failedCheckpoint.InFlightAttemptId);
    }

    /// <summary>
    ///     Verifies terminal saga completion clears pending recovery state.
    /// </summary>
    [Fact]
    public void ReduceClearsPendingStateWhenSagaCompletes()
    {
        DateTimeOffset startedAt = new(2025, 2, 15, 13, 0, 0, TimeSpan.Zero);
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = startedAt,
            },
            new SagaStepExecutionStarted
            {
                AttemptId = Guid.NewGuid(),
                Direction = SagaExecutionDirection.Forward,
                OperationKey = "forward-key",
                Source = SagaResumeSource.Reminder,
                StartedAt = startedAt.AddMinutes(1),
                StepIndex = 0,
                StepName = "Debit",
            },
            new SagaStepCompleted
            {
                AttemptId = Guid.NewGuid(),
                StepIndex = 0,
                StepName = "Debit",
                CompletedAt = startedAt.AddMinutes(2),
                OperationKey = "forward-key",
            },
            new SagaCompleted
            {
                CompletedAt = startedAt.AddMinutes(3),
            });
        Assert.Null(checkpoint.PendingDirection);
        Assert.Null(checkpoint.PendingStepIndex);
        Assert.Null(checkpoint.PendingStepName);
        Assert.Null(checkpoint.InFlightAttemptId);
        Assert.Null(checkpoint.InFlightOperationKey);
        Assert.False(checkpoint.ReminderArmed);
    }

    /// <summary>
    ///     Verifies legacy failed-step events with defaulted replay metadata do not seed invalid in-flight attempt state.
    /// </summary>
    [Fact]
    public void ReduceNormalizesLegacyFailedStepAttemptMetadataDefaults()
    {
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = new(2025, 2, 15, 12, 15, 0, TimeSpan.Zero),
            },
            new SagaStepFailed
            {
                AttemptId = Guid.Empty,
                StepIndex = 1,
                StepName = "Credit",
                ErrorCode = "ERR",
                ErrorMessage = "boom",
                OperationKey = null!,
            });
        Assert.Equal(SagaExecutionDirection.Forward, checkpoint.PendingDirection);
        Assert.Equal(1, checkpoint.PendingStepIndex);
        Assert.Equal("Credit", checkpoint.PendingStepName);
        Assert.Null(checkpoint.InFlightAttemptId);
        Assert.Null(checkpoint.InFlightOperationKey);
    }

    /// <summary>
    ///     Verifies failure and compensation events deterministically move the checkpoint into compensation state.
    /// </summary>
    [Fact]
    public void ReduceSwitchesCheckpointIntoCompensationFlow()
    {
        DateTimeOffset startedAt = new(2025, 2, 15, 12, 0, 0, TimeSpan.Zero);
        Guid attemptId = Guid.NewGuid();
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = startedAt,
            },
            new SagaStepExecutionStarted
            {
                AttemptId = attemptId,
                Direction = SagaExecutionDirection.Forward,
                OperationKey = "forward-key",
                Source = SagaResumeSource.Initial,
                StartedAt = startedAt.AddMinutes(1),
                StepIndex = 1,
                StepName = "Credit",
            },
            new SagaStepFailed
            {
                AttemptId = attemptId,
                StepIndex = 1,
                StepName = "Credit",
                ErrorCode = "ERR",
                ErrorMessage = "boom",
                OperationKey = "forward-key",
            },
            new SagaCompensating
            {
                FromStepIndex = 1,
            },
            new SagaStepExecutionStarted
            {
                AttemptId = Guid.NewGuid(),
                Direction = SagaExecutionDirection.Compensation,
                OperationKey = "comp-key",
                Source = SagaResumeSource.Reminder,
                StartedAt = startedAt.AddMinutes(2),
                StepIndex = 1,
                StepName = "Credit",
            },
            new SagaStepCompensated
            {
                AttemptId = Guid.NewGuid(),
                OperationKey = "comp-key",
                StepIndex = 1,
                StepName = "Credit",
            });
        Assert.Equal(SagaExecutionDirection.Compensation, checkpoint.PendingDirection);
        Assert.Equal(0, checkpoint.PendingStepIndex);
        Assert.Null(checkpoint.PendingStepName);
        Assert.Null(checkpoint.InFlightAttemptId);
        Assert.Null(checkpoint.InFlightOperationKey);
        Assert.Equal(SagaResumeSource.Reminder, checkpoint.LastResumeSource);
        Assert.Equal(startedAt.AddMinutes(2), checkpoint.LastResumeAttemptedAt);
        Assert.Equal(1, checkpoint.AutomaticAttemptCount);
    }

    /// <summary>
    ///     Verifies explicit manual resume events refresh the stored access fingerprint.
    /// </summary>
    [Fact]
    public void ReduceUpdatesAccessFingerprintForManualResumeExecution()
    {
        SagaRecoveryCheckpoint checkpoint = Reduce(
            new SagaStartedEvent
            {
                AccessContextFingerprint = "tenant:user-a",
                SagaId = Guid.NewGuid(),
                RecoveryMode = SagaRecoveryMode.Automatic,
                StepHash = "HASH",
                StartedAt = new(2025, 2, 15, 10, 0, 0, TimeSpan.Zero),
            },
            new SagaStepExecutionStarted
            {
                AccessContextFingerprint = "tenant:user-b",
                AttemptId = Guid.NewGuid(),
                Direction = SagaExecutionDirection.Forward,
                OperationKey = "manual-key",
                Source = SagaResumeSource.Manual,
                StartedAt = new(2025, 2, 15, 10, 1, 0, TimeSpan.Zero),
                StepIndex = 0,
                StepName = "Debit",
            });
        Assert.Equal("tenant:user-b", checkpoint.AccessContextFingerprint);
        Assert.Equal(SagaResumeSource.Manual, checkpoint.LastResumeSource);
    }
}