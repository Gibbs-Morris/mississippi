using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaOrchestrationEffectLoggerExtensions" />.
/// </summary>
public sealed class SagaOrchestrationEffectLoggerExtensionsTests
{
    /// <summary>
    ///     Verifies logger extension methods can be invoked without throwing.
    /// </summary>
    [Fact]
    public void LoggerExtensionsCanBeInvoked()
    {
        ILogger logger = NullLogger.Instance;
        Assert.NotNull(logger);
        logger.SagaStepExecuting("TestSaga", "Step", 1);
        logger.SagaStepCompensating("TestSaga", "Step", 1);
        logger.SagaStepExecutionException(
            "TestSaga",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "correlation-1",
            "saga-1",
            10,
            "Step",
            1,
            "SAGA_STEP_EXCEPTION",
            new InvalidOperationException("boom"));
        logger.SagaStepExecutionFailed(
            "TestSaga",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "correlation-1",
            "saga-1",
            10,
            "Step",
            1,
            "STEP_FAILED",
            "boom");
        logger.SagaStepMetadataMissing(
            "TestSaga",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "correlation-2",
            "saga-2",
            20,
            2,
            "STEP_METADATA_MISSING",
            "Step metadata not found.");
        logger.SagaStepCompensationException(
            "TestSaga",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "correlation-3",
            "saga-3",
            30,
            "Step",
            1,
            "COMPENSATION_EXCEPTION",
            new InvalidOperationException("boom"));
        logger.SagaStepCompensationFailed(
            "TestSaga",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "correlation-3",
            "saga-3",
            30,
            "Step",
            1,
            "COMPENSATION_FAILED",
            "boom");
        logger.SagaStepCompensationMetadataMissing(
            "TestSaga",
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "correlation-4",
            "saga-4",
            40,
            4,
            "COMPENSATION_FAILED",
            "Step metadata not found.");
    }
}