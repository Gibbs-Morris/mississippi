using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Diagnostics;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;

using Moq;


namespace MississippiTests.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests prune operation counting through the snapshot storage provider.
/// </summary>
public sealed class SnapshotStorageProviderPruneMetricsTests
{
    /// <summary>
    ///     Observes prune measurements for one unique snapshot type.
    /// </summary>
    /// <param name="snapshotType">The snapshot type used to isolate this test's measurements.</param>
    /// <param name="measurements">The thread-safe measurement sink.</param>
    /// <returns>The started listener, which the caller owns.</returns>
    private static MeterListener CreatePruneListener(
        string snapshotType,
        ConcurrentQueue<long> measurements
    )
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if ((instrument.Meter.Name == SnapshotStorageMetrics.MeterName) &&
                (instrument.Name == "cosmos.snapshot.prune.count"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if ((tag.Key == "snapshot.type") && (tag.Value as string == snapshotType))
                {
                    measurements.Enqueue(measurement);
                }
            }
        });
        listener.Start();
        return listener;
    }

    /// <summary>
    ///     Creates a unique snapshot stream key for metric isolation.
    /// </summary>
    /// <returns>A stream key with a test-specific snapshot storage name.</returns>
    private static SnapshotStreamKey CreateStreamKey() =>
        new("TEST.BROOK", $"PruneMetrics-{Guid.NewGuid():N}", "id", "hash");

    /// <summary>
    ///     Provides five snapshot versions for representative retention behavior.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>The snapshot identifiers for versions one through five.</returns>
    private static async IAsyncEnumerable<SnapshotIdVersion> ReadSnapshotIdsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (int version = 1; version <= 5; version++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new($"snapshot-{version}", version);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Ensures failed and canceled prune operations do not contribute a completed operation.
    /// </summary>
    /// <param name="isCanceled">Whether the repository operation is canceled.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PruneAsyncShouldNotRecordUnsuccessfulOperations(
        bool isCanceled
    )
    {
        SnapshotStreamKey streamKey = CreateStreamKey();
        int[] retainModuli = [2, 3];
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<ISnapshotCosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.PruneAsync(streamKey, retainModuli, cancellation.Token)).Returns(completion.Task);
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        ConcurrentQueue<long> measurements = new();
        using MeterListener listener = CreatePruneListener(streamKey.SnapshotStorageName, measurements);
        Assert.Empty(measurements.ToArray());
        if (isCanceled)
        {
            await cancellation.CancelAsync();
            completion.SetCanceled(cancellation.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.PruneAsync(
                streamKey,
                retainModuli,
                cancellation.Token));
        }
        else
        {
            completion.SetException(new InvalidOperationException("Prune failed."));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.PruneAsync(
                streamKey,
                retainModuli,
                cancellation.Token));
        }

        repository.Verify(r => r.PruneAsync(streamKey, retainModuli, cancellation.Token), Times.Once);
        Assert.Empty(measurements.ToArray());
    }

    /// <summary>
    ///     Ensures one operation is recorded independently of retention rules and deleted snapshot count.
    /// </summary>
    /// <param name="retentionRuleCount">The number of retention rules supplied.</param>
    /// <param name="deletedSnapshotCount">The expected number of deleted snapshots.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public async Task PruneAsyncShouldRecordOneCompletedOperation(
        int retentionRuleCount,
        int deletedSnapshotCount
    )
    {
        SnapshotStreamKey streamKey = CreateStreamKey();
        string partitionKey = streamKey.ToString();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int[] moduli = [2, 3];
        int[] retainModuli = moduli[..retentionRuleCount];
        Mock<ISnapshotContainerOperations> operations = new(MockBehavior.Strict);
        operations.Setup(o => o.QuerySnapshotIdsAsync(partitionKey, cancellationToken))
            .Returns(ReadSnapshotIdsAsync(cancellationToken));
        operations.Setup(o => o.DeleteDocumentAsync(partitionKey, It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(true);
        SnapshotCosmosRepository repository = new(
            operations.Object,
            Mock.Of<IMapper<SnapshotDocument, SnapshotStorageModel>>(),
            Mock.Of<IMapper<SnapshotStorageModel, SnapshotEnvelope>>(),
            Mock.Of<IMapper<SnapshotWriteModel, SnapshotStorageModel>>(),
            Mock.Of<IMapper<SnapshotStorageModel, SnapshotDocument>>(),
            NullLogger<SnapshotCosmosRepository>.Instance);
        SnapshotStorageProvider provider = new(repository, NullLogger<SnapshotStorageProvider>.Instance);
        ConcurrentQueue<long> measurements = new();
        using MeterListener listener = CreatePruneListener(streamKey.SnapshotStorageName, measurements);
        await provider.PruneAsync(streamKey, retainModuli, cancellationToken);
        operations.Verify(o => o.QuerySnapshotIdsAsync(partitionKey, cancellationToken), Times.Once);
        operations.Verify(
            o => o.DeleteDocumentAsync(partitionKey, It.IsAny<string>(), cancellationToken),
            Times.Exactly(deletedSnapshotCount));
        operations.Verify(o => o.DeleteDocumentAsync(partitionKey, "snapshot-5", cancellationToken), Times.Never);
        long measurement = Assert.Single(measurements.ToArray());
        Assert.Equal(1, measurement);
    }

    /// <summary>
    ///     Ensures a pending prune does not count until the repository completes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldWaitForCompletionBeforeRecording()
    {
        SnapshotStreamKey streamKey = CreateStreamKey();
        int[] retainModuli = [2];
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<ISnapshotCosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.PruneAsync(streamKey, retainModuli, cancellationToken)).Returns(completion.Task);
        SnapshotStorageProvider provider = new(repository.Object, NullLogger<SnapshotStorageProvider>.Instance);
        ConcurrentQueue<long> measurements = new();
        using MeterListener listener = CreatePruneListener(streamKey.SnapshotStorageName, measurements);
        Task prune = provider.PruneAsync(streamKey, retainModuli, cancellationToken);
        Assert.False(prune.IsCompleted);
        Assert.Empty(measurements.ToArray());
        completion.SetResult(true);
        await prune;
        repository.Verify(r => r.PruneAsync(streamKey, retainModuli, cancellationToken), Times.Once);
        long measurement = Assert.Single(measurements.ToArray());
        Assert.Equal(1, measurement);
    }
}