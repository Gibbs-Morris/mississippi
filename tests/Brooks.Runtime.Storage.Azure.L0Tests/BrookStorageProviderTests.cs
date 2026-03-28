using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Brooks;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for Brooks Azure provider orchestration behavior.
/// </summary>
public sealed class BrookStorageProviderTests
{
    private static async IAsyncEnumerable<BrookEvent> AsAsyncEnumerableAsync(
        IEnumerable<BrookEvent> items
    )
    {
        foreach (BrookEvent item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    /// <summary>
    ///     Verifies the provider reports its storage format value.
    /// </summary>
    [Fact]
    public void FormatReturnsAzureBlob()
    {
        BrookStorageProvider provider = new(
            new Mock<IBrookRecoveryService>(MockBehavior.Strict).Object,
            new Mock<IAzureBrookRepository>(MockBehavior.Strict).Object,
            new Mock<IEventBrookWriter>(MockBehavior.Strict).Object,
            NullLogger<BrookStorageProvider>.Instance);

        Assert.Equal("azure-blob", provider.Format);
    }

    /// <summary>
    ///     Verifies append delegates to the writer and returns its result.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncDelegatesToWriter()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookKey brookId = new("type", "id");
        BrookEvent[] events =
        [
            new BrookEvent
            {
                Id = "event-1",
                Source = "tests",
                EventType = "created",
                Data = ImmutableArray<byte>.Empty,
            },
        ];

        eventWriter.Setup(writer => writer.AppendEventsAsync(
                brookId,
                It.Is<IReadOnlyList<BrookEvent>>(value => value.Count == 1),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(4));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        BrookPosition result = await provider.AppendEventsAsync(brookId, events);

        Assert.Equal(4, result.Value);
        eventWriter.VerifyAll();
        recoveryService.VerifyNoOtherCalls();
        repository.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies append rejects an empty event batch.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsWhenEventsEmpty()
    {
        BrookStorageProvider provider = new(
            new Mock<IBrookRecoveryService>(MockBehavior.Strict).Object,
            new Mock<IAzureBrookRepository>(MockBehavior.Strict).Object,
            new Mock<IEventBrookWriter>(MockBehavior.Strict).Object,
            NullLogger<BrookStorageProvider>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => provider.AppendEventsAsync(
            new BrookKey("type", "id"),
            Array.Empty<BrookEvent>()));
    }

    /// <summary>
    ///     Verifies cursor reads delegate to the recovery service.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadCursorPositionAsyncDelegatesToRecoveryService()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookKey brookId = new("type", "id");

        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(12));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        BrookPosition result = await provider.ReadCursorPositionAsync(brookId);

        Assert.Equal(12, result.Value);
        recoveryService.VerifyAll();
        repository.VerifyNoOtherCalls();
        eventWriter.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies cursor reads surface retryable live-writer protection without touching the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadCursorPositionAsyncPropagatesRetryableExceptionWhenRecoveryCannotSafelyMutatePendingState()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookKey brookId = new("type", "id");

        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(brookId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrookStorageRetryableException("retry"));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        await Assert.ThrowsAsync<BrookStorageRetryableException>(() => provider.ReadCursorPositionAsync(brookId));

        recoveryService.VerifyAll();
        repository.VerifyNoOtherCalls();
        eventWriter.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies event reads clamp to the committed cursor before enumerating blobs.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadEventsAsyncReadsOnlyCommittedRange()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookRangeKey requestedRange = new("type", "id", 2, 5);
        List<BrookEvent> expectedEvents =
        [
            new BrookEvent
            {
                Id = "e-2",
                Source = "tests",
                EventType = "created",
                Data = ImmutableArray<byte>.Empty,
            },
            new BrookEvent
            {
                Id = "e-3",
                Source = "tests",
                EventType = "updated",
                Data = ImmutableArray<byte>.Empty,
            },
        ];

        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                requestedRange.ToBrookCompositeKey(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(3));
        repository.Setup(repo => repo.ReadEventsAsync(
                It.Is<BrookRangeKey>(value =>
                    (value.Start.Value == 2) &&
                    (value.Count == 2)),
                It.IsAny<CancellationToken>()))
            .Returns(AsAsyncEnumerableAsync(expectedEvents));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        List<BrookEvent> events = [];
        await foreach (BrookEvent brookEvent in provider.ReadEventsAsync(requestedRange))
        {
            events.Add(brookEvent);
        }

        Assert.Equal(expectedEvents.Select(item => item.Id), events.Select(item => item.Id));
        recoveryService.VerifyAll();
        repository.VerifyAll();
        eventWriter.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies reads return an empty sequence when the request starts after the committed cursor.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadEventsAsyncReturnsEmptyWhenRangeStartsAfterCursor()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookRangeKey requestedRange = new("type", "id", 5, 2);

        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                requestedRange.ToBrookCompositeKey(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(4));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        List<BrookEvent> events = [];
        await foreach (BrookEvent brookEvent in provider.ReadEventsAsync(requestedRange))
        {
            events.Add(brookEvent);
        }

        Assert.Empty(events);
        recoveryService.VerifyAll();
        repository.VerifyNoOtherCalls();
        eventWriter.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies reads surface retryable live-writer protection before any event blobs are enumerated.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadEventsAsyncPropagatesRetryableExceptionWhenRecoveryCannotSafelyMutatePendingState()
    {
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IEventBrookWriter> eventWriter = new(MockBehavior.Strict);
        BrookRangeKey requestedRange = new("type", "id", 1, 2);

        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                requestedRange.ToBrookCompositeKey(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrookStorageRetryableException("retry"));

        BrookStorageProvider provider = new(
            recoveryService.Object,
            repository.Object,
            eventWriter.Object,
            NullLogger<BrookStorageProvider>.Instance);

        BrookStorageRetryableException exception = await Assert.ThrowsAsync<BrookStorageRetryableException>(async () =>
        {
            await foreach (BrookEvent brookEvent in provider.ReadEventsAsync(requestedRange))
            {
                GC.KeepAlive(brookEvent);
            }
        });

        Assert.Equal("retry", exception.Message);
        recoveryService.VerifyAll();
        repository.VerifyNoOtherCalls();
        eventWriter.VerifyNoOtherCalls();
    }
}