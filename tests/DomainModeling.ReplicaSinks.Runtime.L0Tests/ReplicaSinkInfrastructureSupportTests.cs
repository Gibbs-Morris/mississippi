using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.ReplicaSinks.Runtime;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

using Moq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests thin runtime support types that were previously under-covered by the main execution-path suites.
/// </summary>
public sealed class ReplicaSinkInfrastructureSupportTests
{
    /// <summary>
    ///     Ensures audit logging honors cancellation before recording entries.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoggerReplicaSinkOperatorAuditSinkShouldHonorCancellation()
    {
        Mock<ILogger<LoggerReplicaSinkOperatorAuditSink>> logger = new(MockBehavior.Strict);
        LoggerReplicaSinkOperatorAuditSink auditSink = new(logger.Object);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        ReplicaSinkDeadLetterQuery query = new(new("operator-a", ReplicaSinkOperatorAccessLevel.Summary), 1);
        ReplicaSinkDeadLetterReDriveRequest request = new(
            new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin),
            "Projection::sink-a::orders::entity-1");
        ReplicaSinkDeadLetterReDriveResult result = new(request.DeliveryKey, "queued", true, 42);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await auditSink.RecordDeadLetterReadAsync(query, 1, 0, false, false, cancellationTokenSource.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await auditSink.RecordReDriveAsync(request, result, cancellationTokenSource.Token));
        logger.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Ensures dead-letter page reads are logged with the expected audit details.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoggerReplicaSinkOperatorAuditSinkShouldLogDeadLetterReadDetails()
    {
        Mock<ILogger<LoggerReplicaSinkOperatorAuditSink>> logger = new();
        logger.Setup(instance => instance.IsEnabled(LogLevel.Information)).Returns(true);
        LoggerReplicaSinkOperatorAuditSink auditSink = new(logger.Object);
        ReplicaSinkDeadLetterQuery query = new(
            new("operator-a", ReplicaSinkOperatorAccessLevel.Summary),
            5,
            "next-token",
            true);
        await auditSink.RecordDeadLetterReadAsync(query, 3, 2, true, true, CancellationToken.None);
        logger.Verify(
            static instance => instance.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((
                        state,
                        _
                    ) => state.ToString()!.Contains("operator-a", StringComparison.Ordinal) &&
                         state.ToString()!.Contains("Summary", StringComparison.Ordinal) &&
                         state.ToString()!.Contains("requested size 5", StringComparison.Ordinal) &&
                         state.ToString()!.Contains("result count 2", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures re-drive audit entries capture the delivery key and stable outcome.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoggerReplicaSinkOperatorAuditSinkShouldLogReDriveDetails()
    {
        Mock<ILogger<LoggerReplicaSinkOperatorAuditSink>> logger = new();
        logger.Setup(instance => instance.IsEnabled(LogLevel.Information)).Returns(true);
        LoggerReplicaSinkOperatorAuditSink auditSink = new(logger.Object);
        ReplicaSinkDeadLetterReDriveRequest request = new(
            new("operator-admin", ReplicaSinkOperatorAccessLevel.Admin),
            "Projection::sink-a::orders::entity-1");
        ReplicaSinkDeadLetterReDriveResult result = new(request.DeliveryKey, "queued", true, 42);
        await auditSink.RecordReDriveAsync(request, result, CancellationToken.None);
        logger.Verify(
            instance => instance.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((
                        state,
                        _
                    ) => state.ToString()!.Contains("operator-admin", StringComparison.Ordinal) &&
                         state.ToString()!.Contains(request.DeliveryKey, StringComparison.Ordinal) &&
                         state.ToString()!.Contains("queued", StringComparison.Ordinal) &&
                         state.ToString()!.Contains("42", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures stop remains a no-op for the thin startup validator shell.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidationServiceShouldCompleteStopWithoutCallingValidator()
    {
        Mock<IReplicaSinkStartupValidator> startupValidator = new(MockBehavior.Strict);
        ReplicaSinkStartupValidationService service = new(startupValidator.Object);
        await service.StopAsync(CancellationToken.None);
        startupValidator.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Ensures startup validation delegates to the registered validator and preserves cancellation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidationServiceShouldDelegateStartToValidator()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        Mock<IReplicaSinkStartupValidator> startupValidator = new();
        startupValidator.Setup(validator => validator.ValidateAsync(cancellationTokenSource.Token))
            .Returns(ValueTask.CompletedTask)
            .Verifiable();
        ReplicaSinkStartupValidationService service = new(startupValidator.Object);
        await service.StartAsync(cancellationTokenSource.Token);
        startupValidator.Verify();
        startupValidator.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Ensures startup validation failures flow through unchanged.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidationServiceShouldPropagateValidationFailures()
    {
        InvalidOperationException expected = new("validation failed");
        Mock<IReplicaSinkStartupValidator> startupValidator = new();
        startupValidator.Setup(validator => validator.ValidateAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(expected));
        ReplicaSinkStartupValidationService service = new(startupValidator.Object);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.StartAsync(CancellationToken.None));
        Assert.Same(expected, actual);
        startupValidator.Verify(validator => validator.ValidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        startupValidator.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Ensures explicit metadata survives construction for downstream persistence handling.
    /// </summary>
    [Fact]
    public void ReplicaSinkWriteExceptionShouldPreserveExplicitMetadata()
    {
        InvalidOperationException innerException = new("durable failure");
        ReplicaSinkWriteException exception = new(
            ReplicaSinkWriteFailureDisposition.DeadLetter,
            "dead_letter",
            "Persisted safe summary.",
            innerException);
        Assert.Equal("Persisted safe summary.", exception.Message);
        Assert.Equal(ReplicaSinkWriteFailureDisposition.DeadLetter, exception.Disposition);
        Assert.Equal("dead_letter", exception.FailureCode);
        Assert.Equal("Persisted safe summary.", exception.FailureSummary);
        Assert.Same(innerException, exception.InnerException);
    }

    /// <summary>
    ///     Ensures invalid explicit metadata is rejected before the exception can be persisted.
    /// </summary>
    [Fact]
    public void ReplicaSinkWriteExceptionShouldRejectInvalidExplicitMetadata()
    {
        Assert.Throws<ArgumentNullException>(() => new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            null!,
            "summary"));
        Assert.Throws<ArgumentNullException>(() => new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            "code",
            null!));
        Assert.Throws<ArgumentException>(() => new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            " ",
            "summary"));
        Assert.Throws<ArgumentException>(() => new ReplicaSinkWriteException(
            ReplicaSinkWriteFailureDisposition.Retry,
            "code",
            " "));
        Assert.Throws<ArgumentNullException>(() => new ReplicaSinkWriteException("message", null!));
    }

    /// <summary>
    ///     Ensures the default write exception preserves retry-safe metadata.
    /// </summary>
    [Fact]
    public void ReplicaSinkWriteExceptionShouldUseDefaultMessageAndMetadata()
    {
        ReplicaSinkWriteException exception = new();
        Assert.Equal("Provider write failed.", exception.Message);
        Assert.Equal(ReplicaSinkWriteFailureDisposition.Retry, exception.Disposition);
        Assert.Equal("provider_write_failed", exception.FailureCode);
        Assert.Equal("Provider write failed.", exception.FailureSummary);
    }

    /// <summary>
    ///     Ensures message-based constructors preserve retry metadata and the inner exception relationship.
    /// </summary>
    [Fact]
    public void ReplicaSinkWriteExceptionShouldUseRetryMetadataForMessageConstructors()
    {
        InvalidOperationException innerException = new("inner failure");
        ReplicaSinkWriteException messageOnly = new("custom failure");
        ReplicaSinkWriteException withInner = new("custom failure with inner", innerException);
        Assert.Equal("custom failure", messageOnly.Message);
        Assert.Equal(ReplicaSinkWriteFailureDisposition.Retry, messageOnly.Disposition);
        Assert.Equal("provider_write_failed", messageOnly.FailureCode);
        Assert.Equal("custom failure", messageOnly.FailureSummary);
        Assert.Same(innerException, withInner.InnerException);
        Assert.Equal("custom failure with inner", withInner.FailureSummary);
    }
}