using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for saga abstraction types.
/// </summary>
public sealed class SagaAbstractionsTests
{
    /// <summary>
    ///     Verifies saga step info registration adds a provider.
    /// </summary>
    [Fact]
    public void AddSagaStepInfoRegistersProvider()
    {
        ServiceCollection services = new();
        List<SagaStepInfo> steps =
        [
            new(0, "Step", typeof(SagaStepMarker), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        IServiceCollection result = services.AddSagaStepInfo<TestSagaState>(steps);
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaStepInfoProvider<TestSagaState> resolved =
            provider.GetRequiredService<ISagaStepInfoProvider<TestSagaState>>();
        Assert.Same(steps, resolved.Steps);
    }

    /// <summary>
    ///     Verifies saga recovery info registration adds a provider.
    /// </summary>
    [Fact]
    public void AddSagaRecoveryInfoRegistersProvider()
    {
        ServiceCollection services = new();
        SagaRecoveryInfo recovery = new(SagaRecoveryMode.ManualOnly, "critical-payments");
        IServiceCollection result = services.AddSagaRecoveryInfo<TestSagaState>(recovery);
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRecoveryInfoProvider<TestSagaState> resolved =
            provider.GetRequiredService<ISagaRecoveryInfoProvider<TestSagaState>>();
        Assert.Same(recovery, resolved.Recovery);
    }

    /// <summary>
    ///     Verifies saga step info registration rejects null services.
    /// </summary>
    [Fact]
    public void AddSagaStepInfoThrowsWhenServicesNull()
    {
        IServiceCollection? services = null;
        List<SagaStepInfo> steps =
        [
            new(0, "Step", typeof(SagaStepMarker), false, SagaStepRecoveryPolicy.Automatic, null),
        ];
        Assert.Throws<ArgumentNullException>(() => services!.AddSagaStepInfo<TestSagaState>(steps));
    }

    /// <summary>
    ///     Verifies saga recovery info registration rejects null services.
    /// </summary>
    [Fact]
    public void AddSagaRecoveryInfoThrowsWhenServicesNull()
    {
        IServiceCollection? services = null;
        SagaRecoveryInfo recovery = new(SagaRecoveryMode.Automatic, null);
        Assert.Throws<ArgumentNullException>(() => services!.AddSagaRecoveryInfo<TestSagaState>(recovery));
    }

    /// <summary>
    ///     Verifies saga step info registration rejects null steps.
    /// </summary>
    [Fact]
    public void AddSagaStepInfoThrowsWhenStepsNull()
    {
        ServiceCollection services = new();
        IReadOnlyList<SagaStepInfo>? steps = null;
        Assert.Throws<ArgumentNullException>(() => services.AddSagaStepInfo<TestSagaState>(steps!));
    }

    /// <summary>
    ///     Verifies failed compensation results capture error details.
    /// </summary>
    [Fact]
    public void CompensationResultFailedCapturesErrorDetails()
    {
        CompensationResult result = CompensationResult.Failed("FAIL", "broken");
        Assert.False(result.Success);
        Assert.False(result.Skipped);
        Assert.Equal("FAIL", result.ErrorCode);
        Assert.Equal("broken", result.ErrorMessage);
    }

    /// <summary>
    ///     Verifies skipped compensation results capture the reason.
    /// </summary>
    [Fact]
    public void CompensationResultSkippedCapturesReason()
    {
        CompensationResult result = CompensationResult.SkippedResult("no-op");
        Assert.False(result.Success);
        Assert.True(result.Skipped);
        Assert.Equal("no-op", result.ErrorMessage);
    }

    /// <summary>
    ///     Verifies successful compensation results set success.
    /// </summary>
    [Fact]
    public void CompensationResultSucceededSetsSuccess()
    {
        CompensationResult result = CompensationResult.Succeeded();
        Assert.True(result.Success);
        Assert.False(result.Skipped);
    }

    /// <summary>
    ///     Verifies saga step attributes capture order and saga type.
    /// </summary>
    [Fact]
    public void SagaStepAttributeCapturesOrderAndSaga()
    {
        SagaStepAttribute<TestSagaState> attribute = new(3, SagaStepRecoveryPolicy.ManualOnly)
        {
            CompensationRecoveryPolicy = SagaStepRecoveryPolicy.Automatic,
        };
        Assert.Equal(3, attribute.Order);
        Assert.Equal(typeof(TestSagaState), attribute.Saga);
        Assert.Equal(SagaStepRecoveryPolicy.ManualOnly, attribute.ForwardRecoveryPolicy);
        Assert.Equal(SagaStepRecoveryPolicy.Automatic, attribute.CompensationRecoveryPolicy);
    }

    /// <summary>
    ///     Verifies saga recovery attributes capture mode and profile.
    /// </summary>
    [Fact]
    public void SagaRecoveryAttributeCapturesModeAndProfile()
    {
        SagaRecoveryAttribute attribute = new(SagaRecoveryMode.ManualOnly)
        {
            Profile = "critical-payments",
        };
        Assert.Equal(SagaRecoveryMode.ManualOnly, attribute.Mode);
        Assert.Equal("critical-payments", attribute.Profile);
    }

    /// <summary>
    ///     Verifies step compensated event stores step details.
    /// </summary>
    [Fact]
    public void SagaStepCompensatedStoresStepDetails()
    {
        Guid attemptId = Guid.NewGuid();
        SagaStepCompensated @event = new()
        {
            AttemptId = attemptId,
            OperationKey = "operation-key",
            StepIndex = 1,
            StepName = "Debit",
        };
        Assert.Equal(attemptId, @event.AttemptId);
        Assert.Equal("operation-key", @event.OperationKey);
        Assert.Equal(1, @event.StepIndex);
        Assert.Equal("Debit", @event.StepName);
    }

    /// <summary>
    ///     Verifies step failed event stores error details.
    /// </summary>
    [Fact]
    public void SagaStepFailedStoresErrorDetails()
    {
        Guid attemptId = Guid.NewGuid();
        SagaStepFailed @event = new()
        {
            AttemptId = attemptId,
            StepIndex = 2,
            StepName = "Credit",
            ErrorCode = "ERR",
            ErrorMessage = "Bad",
            OperationKey = "operation-key",
        };
        Assert.Equal(attemptId, @event.AttemptId);
        Assert.Equal(2, @event.StepIndex);
        Assert.Equal("Credit", @event.StepName);
        Assert.Equal("ERR", @event.ErrorCode);
        Assert.Equal("Bad", @event.ErrorMessage);
        Assert.Equal("operation-key", @event.OperationKey);
    }

    /// <summary>
    ///     Verifies step execution-started event stores attempt metadata.
    /// </summary>
    [Fact]
    public void SagaStepExecutionStartedStoresAttemptMetadata()
    {
        Guid attemptId = Guid.NewGuid();
        DateTimeOffset startedAt = new(2026, 4, 4, 13, 0, 0, TimeSpan.Zero);
        SagaStepExecutionStarted @event = new()
        {
            AttemptId = attemptId,
            Direction = SagaExecutionDirection.Forward,
            OperationKey = "operation-key",
            Source = SagaResumeSource.Initial,
            StartedAt = startedAt,
            StepIndex = 3,
            StepName = "Debit",
        };
        Assert.Equal(attemptId, @event.AttemptId);
        Assert.Equal(SagaExecutionDirection.Forward, @event.Direction);
        Assert.Equal("operation-key", @event.OperationKey);
        Assert.Equal(SagaResumeSource.Initial, @event.Source);
        Assert.Equal(startedAt, @event.StartedAt);
        Assert.Equal(3, @event.StepIndex);
        Assert.Equal("Debit", @event.StepName);
    }

    /// <summary>
    ///     Verifies the default AppliesTo implementation returns true.
    /// </summary>
    [Fact]
    public void SagaStepInfoProviderDefaultsToAppliesToTrue()
    {
        DefaultStepInfoProvider provider = new();
        bool appliesTo = ((ISagaStepInfoProvider<TestSagaState>)provider).AppliesTo(new());
        Assert.True(appliesTo);
    }

    /// <summary>
    ///     Verifies saga step execution context stores execution metadata.
    /// </summary>
    [Fact]
    public void SagaStepExecutionContextStoresExecutionMetadata()
    {
        Guid sagaId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        DateTimeOffset startedAt = new(2026, 4, 4, 12, 0, 0, TimeSpan.Zero);
        SagaStepExecutionContext context = new()
        {
            AttemptId = attemptId,
            AttemptStartedAt = startedAt,
            Direction = SagaExecutionDirection.Compensation,
            IsReplay = true,
            OperationKey = "operation-key",
            SagaId = sagaId,
            Source = SagaResumeSource.Manual,
            StepIndex = 7,
            StepName = "Refund",
        };
        Assert.Equal(attemptId, context.AttemptId);
        Assert.Equal(startedAt, context.AttemptStartedAt);
        Assert.Equal(SagaExecutionDirection.Compensation, context.Direction);
        Assert.True(context.IsReplay);
        Assert.Equal("operation-key", context.OperationKey);
        Assert.Equal(sagaId, context.SagaId);
        Assert.Equal(SagaResumeSource.Manual, context.Source);
        Assert.Equal(7, context.StepIndex);
        Assert.Equal("Refund", context.StepName);
    }

    /// <summary>
    ///     Verifies saga step info rejects missing compensation recovery policy when compensation is enabled.
    /// </summary>
    [Fact]
    public void SagaStepInfoThrowsWhenCompensationPolicyMissingForCompensatableStep()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new SagaStepInfo(
                0,
                "Debit",
                typeof(SagaStepMarker),
                true,
                SagaStepRecoveryPolicy.Automatic,
                null));
        Assert.Equal("compensationRecoveryPolicy", exception.ParamName);
    }

    /// <summary>
    ///     Verifies saga step info rejects compensation recovery policy for non-compensatable steps.
    /// </summary>
    [Fact]
    public void SagaStepInfoThrowsWhenCompensationPolicyProvidedForNonCompensatableStep()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new SagaStepInfo(
                0,
                "Debit",
                typeof(SagaStepMarker),
                false,
                SagaStepRecoveryPolicy.Automatic,
                SagaStepRecoveryPolicy.ManualOnly));
        Assert.Equal("compensationRecoveryPolicy", exception.ParamName);
    }

    /// <summary>
    ///     Verifies failed step results capture error details.
    /// </summary>
    [Fact]
    public void StepResultFailedCapturesErrorDetails()
    {
        StepResult result = StepResult.Failed("STEP_FAIL", "oops");
        Assert.False(result.Success);
        Assert.Equal("STEP_FAIL", result.ErrorCode);
        Assert.Equal("oops", result.ErrorMessage);
        Assert.Empty(result.Events);
    }

    /// <summary>
    ///     Verifies successful step results include events.
    /// </summary>
    [Fact]
    public void StepResultSucceededCapturesEvents()
    {
        object evt = new SagaAbstractionsMarkerEvent();
        StepResult result = StepResult.Succeeded(evt);
        Assert.True(result.Success);
        Assert.Same(evt, Assert.Single(result.Events));
    }
}