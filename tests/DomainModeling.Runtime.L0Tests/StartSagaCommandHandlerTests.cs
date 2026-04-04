using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="StartSagaCommandHandler{TSaga,TInput}" />.
/// </summary>
public sealed class StartSagaCommandHandlerTests
{
    private static string ComputeExpectedStepHash(
        SagaRecoveryInfo recovery,
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        StringBuilder builder = new();
        builder.Append("Recovery=").Append(recovery.Mode).Append(':');
        if (recovery.Profile is null)
        {
            builder.Append("Profile:null");
        }
        else
        {
            builder.Append("Profile:value:").Append(recovery.Profile.Length).Append(':').Append(recovery.Profile);
        }

        for (int i = 0; i < steps.Count; i++)
        {
            SagaStepInfo step = steps[i];
            builder.Append('|');
            string stepTypeName = step.StepType.FullName ?? step.StepType.Name;
            builder.Append(step.StepIndex)
                .Append(':')
                .Append(step.StepName)
                .Append(':')
                .Append(stepTypeName)
                .Append(':')
                .Append(step.HasCompensation)
                .Append(':')
                .Append(step.ForwardRecoveryPolicy)
                .Append(':')
                .Append(step.CompensationRecoveryPolicy?.ToString() ?? "NONE");
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

    private static SagaRecoveryInfoProvider<TestSagaState> CreateRecoveryInfoProvider(
        SagaRecoveryInfo? recovery = null
    ) =>
        new(recovery ?? new SagaRecoveryInfo(SagaRecoveryMode.Automatic, null));

    private sealed class CreditStep : ISagaStep<TestSagaState>
    {
        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            SagaStepExecutionContext context,
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
            SagaStepExecutionContext context,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(CompensationResult.Succeeded());

        public Task<StepResult> ExecuteAsync(
            TestSagaState state,
            SagaStepExecutionContext context,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(StepResult.Succeeded());
    }

    private sealed class FixedSagaAccessContextProvider(string? fingerprint) : ISagaAccessContextProvider
    {
        public string? GetFingerprint() => fingerprint;
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
            new FixedSagaAccessContextProvider(null),
            new SagaStepInfoProvider<TestSagaState>(Array.Empty<SagaStepInfo>()),
            CreateRecoveryInfoProvider(),
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
            new(
                0,
                "Debit",
                typeof(DebitStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.ManualOnly),
        ];
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new FixedSagaAccessContextProvider(null),
            new SagaStepInfoProvider<TestSagaState>(steps),
            CreateRecoveryInfoProvider(),
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
    ///     Verifies null and literal NONE recovery profiles produce different workflow hashes.
    /// </summary>
    [Fact]
    public void HandleProducesDistinctHashesForNullAndLiteralNoneProfiles()
    {
        DateTimeOffset now = new(2025, 2, 10, 9, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(now);
        IReadOnlyList<SagaStepInfo> steps =
        [
            new(
                0,
                "Debit",
                typeof(DebitStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.ManualOnly),
        ];
        StartSagaCommand<TestInput> command = new()
        {
            SagaId = Guid.NewGuid(),
            Input = new("transfer-1"),
        };
        StartSagaCommandHandler<TestSagaState, TestInput> nullProfileHandler = new(
            new FixedSagaAccessContextProvider(null),
            new SagaStepInfoProvider<TestSagaState>(steps),
            CreateRecoveryInfoProvider(new(SagaRecoveryMode.Automatic, null)),
            timeProvider);
        StartSagaCommandHandler<TestSagaState, TestInput> literalProfileHandler = new(
            new FixedSagaAccessContextProvider(null),
            new SagaStepInfoProvider<TestSagaState>(steps),
            CreateRecoveryInfoProvider(new(SagaRecoveryMode.Automatic, "NONE")),
            timeProvider);
        OperationResult<IReadOnlyList<object>> nullProfileResult = nullProfileHandler.Handle(command, null);
        OperationResult<IReadOnlyList<object>> literalProfileResult = literalProfileHandler.Handle(command, null);
        Assert.True(nullProfileResult.Success);
        Assert.True(literalProfileResult.Success);
        Assert.NotNull(nullProfileResult.Value);
        Assert.NotNull(literalProfileResult.Value);
        SagaStartedEvent nullProfileStarted = Assert.IsType<SagaStartedEvent>(nullProfileResult.Value[0]);
        SagaStartedEvent literalProfileStarted = Assert.IsType<SagaStartedEvent>(literalProfileResult.Value[0]);
        Assert.NotEqual(nullProfileStarted.StepHash, literalProfileStarted.StepHash);
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
            new(
                0,
                "Debit",
                typeof(DebitStep),
                true,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.ManualOnly),
            new(1, "Credit", typeof(CreditStep), false, SagaStepRecoveryPolicy.ManualOnly, null),
        ];
        SagaRecoveryInfo recovery = new(SagaRecoveryMode.ManualOnly, "critical-payments");
        StartSagaCommandHandler<TestSagaState, TestInput> handler = new(
            new FixedSagaAccessContextProvider("tenant:user-a"),
            new SagaStepInfoProvider<TestSagaState>(steps),
            CreateRecoveryInfoProvider(recovery),
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
        Assert.Equal("tenant:user-a", started.AccessContextFingerprint);
        Assert.Equal(command.CorrelationId, started.CorrelationId);
        Assert.Equal(recovery.Mode, started.RecoveryMode);
        Assert.Equal(recovery.Profile, started.RecoveryProfile);
        Assert.Equal(now, started.StartedAt);
        Assert.Equal(ComputeExpectedStepHash(recovery, steps), started.StepHash);
        Assert.Equal(command.SagaId, inputProvided.SagaId);
        Assert.Equal(command.Input, inputProvided.Input);
    }
}