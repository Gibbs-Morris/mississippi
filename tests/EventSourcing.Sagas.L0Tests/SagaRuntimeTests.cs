using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.TimeProvider.Testing;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas;
using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas.L0Tests;

public sealed class StartSagaCommandHandlerTests
{
    [Fact]
    public void Handle_ReturnsSagaStartedEvent()
    {
        DateTimeOffset now = new(2025, 2, 10, 9, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        IReadOnlyList<SagaStepInfo> steps =
        [
            new SagaStepInfo(0, "Debit", typeof(DebitStep), true),
            new SagaStepInfo(1, "Credit", typeof(CreditStep), false),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new TestInput("transfer-1"),
            CorrelationId = "corr-123",
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        Assert.True(result.Success);
        SagaStartedEvent started = Assert.IsType<SagaStartedEvent>(Assert.Single(result.Value));
        Assert.Equal(command.SagaId, started.SagaId);
        Assert.Equal(command.CorrelationId, started.CorrelationId);
        Assert.Equal(now, started.StartedAt);
        Assert.Equal(ComputeExpectedStepHash(steps), started.StepHash);
    }

    [Fact]
    public void Handle_FailsWhenSagaAlreadyStarted()
    {
        FakeTimeProvider timeProvider = new();
        IReadOnlyList<SagaStepInfo> steps =
        [
            new SagaStepInfo(0, "Debit", typeof(DebitStep), true),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new TestInput("transfer-1"),
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    [Fact]
    public void Handle_FailsWhenNoStepsRegistered()
    {
        FakeTimeProvider timeProvider = new();
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(Array.Empty<SagaStepInfo>()),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new TestInput("transfer-1"),
        };

        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    private static string ComputeExpectedStepHash(
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        StringBuilder builder = new();
        for (int i = 0; i < steps.Count; i++)
        {
            SagaStepInfo step = steps[i];
            if (i > 0)
            {
                builder.Append('|');
            }

            string stepTypeName = step.StepType.FullName ?? step.StepType.Name;
            builder.Append(step.StepIndex)
                .Append(':')
                .Append(step.StepName)
                .Append(':')
                .Append(stepTypeName)
                .Append(':')
                .Append(step.HasCompensation);
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

    private sealed class DebitStep : ISagaStep<TestSagaState>, ICompensatable<TestSagaState>
    {
        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken ct
        ) => Task.FromResult(StepResult.Succeeded());

        public Task<CompensationResult> CompensateAsync(
            TestSagaState state,
            CancellationToken ct
        ) => Task.FromResult(CompensationResult.Succeeded());
    }

    private sealed class CreditStep : ISagaStep<TestSagaState>
    {
        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken ct
        ) => Task.FromResult(StepResult.Succeeded());
    }

    private sealed record TestInput(string TransferId);
}

public sealed class SagaInfrastructureReducerTests
{
    [Fact]
    public void SagaStartedReducer_UpdatesState()
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

    [Fact]
    public void SagaStepCompletedReducer_UpdatesStepIndex()
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
            CompletedAt = new DateTimeOffset(2025, 2, 11, 11, 0, 0, TimeSpan.Zero),
        };

        TestSagaState updated = reducer.Reduce(initial, @event);

        Assert.Equal(2, updated.LastCompletedStepIndex);
    }

    [Fact]
    public void SagaCompensatingReducer_SetsPhase()
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

    [Fact]
    public void SagaCompletedReducer_SetsPhase()
    {
        SagaCompletedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompleted @event = new()
        {
            CompletedAt = new DateTimeOffset(2025, 2, 11, 12, 0, 0, TimeSpan.Zero),
        };

        TestSagaState updated = reducer.Reduce(initial, @event);

        Assert.Equal(SagaPhase.Completed, updated.Phase);
    }

    [Fact]
    public void SagaCompensatedReducer_SetsPhase()
    {
        SagaCompensatedReducer<TestSagaState> reducer = new();
        TestSagaState initial = new()
        {
            Phase = SagaPhase.Compensating,
        };
        SagaCompensated @event = new()
        {
            CompletedAt = new DateTimeOffset(2025, 2, 11, 12, 30, 0, TimeSpan.Zero),
        };

        TestSagaState updated = reducer.Reduce(initial, @event);

        Assert.Equal(SagaPhase.Compensated, updated.Phase);
    }

    [Fact]
    public void SagaFailedReducer_SetsPhase()
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
            FailedAt = new DateTimeOffset(2025, 2, 11, 13, 0, 0, TimeSpan.Zero),
        };

        TestSagaState updated = reducer.Reduce(initial, @event);

        Assert.Equal(SagaPhase.Failed, updated.Phase);
    }
}

public sealed record TestSagaState : ISagaState
{
    public Guid SagaId { get; init; }

    public SagaPhase Phase { get; init; }

    public int LastCompletedStepIndex { get; init; } = -1;

    public string? CorrelationId { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public string? StepHash { get; init; }

    public string Name { get; init; } = string.Empty;
}
