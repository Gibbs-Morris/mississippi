using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Tests for <see cref="StartSagaCommandHandler{TSaga,TInput}" />.
/// </summary>
public sealed class StartSagaCommandHandlerTests
{
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

    private sealed class CreditStep : ISagaStep<TestSagaState>
    {
        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    private sealed class DebitStep
        : ISagaStep<TestSagaState>,
          ICompensatable<TestSagaState>
    {
        public Task<CompensationResult> CompensateAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Succeeded());

        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    private sealed record TestInput(string TransferId);

    /// <summary>
    ///     Verifies the handler fails when no steps are registered.
    /// </summary>
    [Fact]
    public void HandleFailsWhenNoStepsRegistered()
    {
        FakeTimeProvider timeProvider = new();
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(Array.Empty<SagaStepInfo>()),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies the handler fails when the saga is already started.
    /// </summary>
    [Fact]
    public void HandleFailsWhenSagaAlreadyStarted()
    {
        FakeTimeProvider timeProvider = new();
        IReadOnlyList<SagaStepInfo> steps =
        [
            new(0, "Debit", typeof(DebitStep), true),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies the handler emits a saga-started event for new sagas.
    /// </summary>
    [Fact]
    public void HandleReturnsSagaStartedEvent()
    {
        DateTimeOffset now = new(2025, 2, 10, 9, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        IReadOnlyList<SagaStepInfo> steps =
        [
            new(0, "Debit", typeof(DebitStep), true),
            new(1, "Credit", typeof(CreditStep), false),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new SagaStepInfoProvider<TestSagaState>(steps),
            timeProvider);
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
            CorrelationId = "corr-123",
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);
        Assert.True(result.Success);
        Assert.Equal(2, result.Value.Count);
        SagaStartedEvent started = Assert.IsType<SagaStartedEvent>(result.Value[0]);
        SagaInputProvided<TestInput> inputProvided = Assert.IsType<SagaInputProvided<TestInput>>(result.Value[1]);
        Assert.Equal(command.SagaId, started.SagaId);
        Assert.Equal(command.CorrelationId, started.CorrelationId);
        Assert.Equal(now, started.StartedAt);
        Assert.Equal(ComputeExpectedStepHash(steps), started.StepHash);
        Assert.Equal(command.SagaId, inputProvided.SagaId);
        Assert.Equal(command.Input, inputProvided.Input);
    }
}