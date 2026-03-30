using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.ReplicaSinks.Runtime;

using Moq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests the replica-sink runtime execution hosted service.
/// </summary>
public sealed class ReplicaSinkRuntimeExecutionServiceTests
{
    /// <summary>
    ///     Ensures the execution pump logs the concrete exception object when a batch iteration fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecutionPumpShouldLogExceptionObjectWhenBatchExecutionFails()
    {
        FakeTimeProvider timeProvider = new();
        TaskCompletionSource<bool> invocationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InvalidOperationException failure = new("Synthetic coordinator failure.");
        Mock<IReplicaSinkRuntimeCoordinator> coordinator = new();
        coordinator.Setup(runtimeCoordinator => runtimeCoordinator.ExecuteBatchAsync(It.IsAny<CancellationToken>()))
            .Callback(() => invocationSource.TrySetResult(true))
            .ThrowsAsync(failure);
        Mock<ILogger<ReplicaSinkRuntimeExecutionService>> logger = new();
        logger.Setup(log => log.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        using ReplicaSinkRuntimeExecutionService service = new(
            coordinator.Object,
            Options.Create(new ReplicaSinkRuntimeOptions
            {
                ExecutionPollInterval = TimeSpan.FromMinutes(5),
            }),
            timeProvider,
            logger.Object);
        await service.StartAsync(CancellationToken.None);
        await invocationSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);
        logger.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.Is<EventId>(id =>
                    (id.Id == 30) &&
                    (id.Name == nameof(ReplicaSinkRuntimeExecutionServiceLoggerExtensions.ExecutionPumpIterationFailed))),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => state.ToString()!.Contains("Replica sink runtime execution pump iteration failed.", StringComparison.Ordinal)),
                It.Is<Exception>(ex => ReferenceEquals(ex, failure)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
