using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

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
    }
}