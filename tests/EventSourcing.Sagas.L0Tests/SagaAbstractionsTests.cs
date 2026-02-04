using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

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
            new(0, "Step", typeof(SagaStepMarker), false),
        ];
        IServiceCollection result = services.AddSagaStepInfo<TestSagaState>(steps);
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaStepInfoProvider<TestSagaState> resolved =
            provider.GetRequiredService<ISagaStepInfoProvider<TestSagaState>>();
        Assert.Same(steps, resolved.Steps);
    }

    /// <summary>
    ///     Verifies saga step info registration rejects null services.
    /// </summary>
    [Fact]
    public void AddSagaStepInfoThrowsWhenServicesNull()
    {
        IServiceCollection? services = null;
        List<SagaStepInfo> steps = [new(0, "Step", typeof(SagaStepMarker), false)];
        Assert.Throws<ArgumentNullException>(() => services!.AddSagaStepInfo<TestSagaState>(steps));
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
        SagaStepAttribute<TestSagaState> attribute = new(3);
        Assert.Equal(3, attribute.Order);
        Assert.Equal(typeof(TestSagaState), attribute.Saga);
    }

    /// <summary>
    ///     Verifies step compensated event stores step details.
    /// </summary>
    [Fact]
    public void SagaStepCompensatedStoresStepDetails()
    {
        SagaStepCompensated @event = new()
        {
            StepIndex = 1,
            StepName = "Debit",
        };
        Assert.Equal(1, @event.StepIndex);
        Assert.Equal("Debit", @event.StepName);
    }

    /// <summary>
    ///     Verifies step failed event stores error details.
    /// </summary>
    [Fact]
    public void SagaStepFailedStoresErrorDetails()
    {
        SagaStepFailed @event = new()
        {
            StepIndex = 2,
            StepName = "Credit",
            ErrorCode = "ERR",
            ErrorMessage = "Bad",
        };
        Assert.Equal(2, @event.StepIndex);
        Assert.Equal("Credit", @event.StepName);
        Assert.Equal("ERR", @event.ErrorCode);
        Assert.Equal("Bad", @event.ErrorMessage);
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