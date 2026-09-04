using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs;
using Mississippi.Tributary.Runtime.Storage.Blobs.Diagnostics;

using MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests.Diagnostics;

using Moq;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotBlobStorageProvider" />.
/// </summary>
public sealed class SnapshotBlobStorageProviderTests
{
    private static readonly SnapshotStreamKey StreamKey = new(
        "TEST.BROOK",
        "BankAccountBalance",
        "acct-123",
        "reducers-hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 5);

    private static MeterListener CreateMetricsListener(
        string snapshotType,
        ConcurrentQueue<MetricMeasurement> measurements
    )
    {
        MeterListener listener = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) &&
                string.Equals(measuredSnapshotType as string, snapshotType, StringComparison.Ordinal))
            {
                measurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
            }
        });
        listener.Start();
        return listener;
    }

    /// <summary>
    ///     Verifies constructor argument validation.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotBlobStorageProvider(
            null!,
            NullLogger<SnapshotBlobStorageProvider>.Instance));
    }

    /// <summary>
    ///     Verifies delete-all delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.DeleteAllAsync(StreamKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAllAsync(StreamKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies delete delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsyncShouldDelegate()
    {
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.DeleteAsync(SnapshotKey, CancellationToken.None);
        repository.Verify(r => r.DeleteAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies the provider format identifier is stable.
    /// </summary>
    [Fact]
    public void FormatShouldReturnAzureBlob()
    {
        SnapshotBlobStorageProvider provider = new(
            Mock.Of<ISnapshotBlobRepository>(),
            NullLogger<SnapshotBlobStorageProvider>.Instance);
        Assert.Equal("azure-blob", provider.Format);
    }

    /// <summary>
    ///     Verifies prune validates arguments and delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldDelegate()
    {
        List<int> retainModuli =
        [
            2,
        ];
        Mock<ISnapshotBlobRepository> repository = new();
        repository.Setup(r => r.PruneAsync(StreamKey, retainModuli, CancellationToken.None)).ReturnsAsync(0);
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.PruneAsync(StreamKey, retainModuli, CancellationToken.None);
        repository.Verify(r => r.PruneAsync(StreamKey, retainModuli, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies prune rejects null retain sets.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task PruneAsyncShouldThrowWhenRetainModuliNull()
    {
        SnapshotBlobStorageProvider provider = new(
            Mock.Of<ISnapshotBlobRepository>(),
            NullLogger<SnapshotBlobStorageProvider>.Instance);
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.PruneAsync(
            StreamKey,
            null!,
            CancellationToken.None));
    }

    /// <summary>
    ///     Verifies read delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        repository.Setup(r => r.ReadAsync(SnapshotKey, CancellationToken.None)).ReturnsAsync(envelope);
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        SnapshotEnvelope? result = await provider.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Same(envelope, result);
        repository.Verify(r => r.ReadAsync(SnapshotKey, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies absent snapshots remain absent and are recorded as not found in provider metrics.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadAsyncShouldRecordNotFoundWhenSnapshotIsMissing()
    {
        const string snapshotType = nameof(ReadAsyncShouldRecordNotFoundWhenSnapshotIsMissing);
        SnapshotKey snapshotKey = new(
            new(StreamKey.BrookName, snapshotType, StreamKey.EntityId, StreamKey.ReducersHash),
            5);
        ConcurrentQueue<MetricMeasurement> measurements = new();
        using MeterListener listener = CreateMetricsListener(snapshotType, measurements);
        using CancellationTokenSource cancellation = new();
        Mock<ISnapshotBlobRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.ReadAsync(snapshotKey, cancellation.Token))
            .ReturnsAsync((SnapshotEnvelope?)null);
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        SnapshotEnvelope? result = await provider.ReadAsync(snapshotKey, cancellation.Token);
        Assert.Null(result);
        MetricMeasurement count = Assert.Single(
            measurements.ToArray(),
            measurement => measurement.InstrumentName == "blob.snapshot.read.count");
        Assert.Equal(1, count.LongValue);
        Assert.Equal("not_found", count.Tags["result"]);
        repository.Verify(value => value.ReadAsync(snapshotKey, cancellation.Token), Times.Once);
    }

    /// <summary>
    ///     Verifies write delegates to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldDelegate()
    {
        SnapshotEnvelope envelope = new();
        Mock<ISnapshotBlobRepository> repository = new();
        SnapshotBlobStorageProvider provider = new(repository.Object, NullLogger<SnapshotBlobStorageProvider>.Instance);
        await provider.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        repository.Verify(r => r.WriteAsync(SnapshotKey, envelope, CancellationToken.None), Times.Once);
    }

    /// <summary>
    ///     Verifies failed writes preserve the storage exception and record failure metrics and snapshot context.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriteAsyncShouldPreserveFailureAndRecordDiagnostics()
    {
        const string snapshotType = nameof(WriteAsyncShouldPreserveFailureAndRecordDiagnostics);
        SnapshotKey snapshotKey = new(
            new(StreamKey.BrookName, snapshotType, StreamKey.EntityId, StreamKey.ReducersHash),
            5);
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataSizeBytes = 3,
            ReducerHash = StreamKey.ReducersHash,
        };
        ConcurrentQueue<MetricMeasurement> measurements = new();
        using MeterListener listener = CreateMetricsListener(snapshotType, measurements);
        using CancellationTokenSource cancellation = new();
        IOException failure = new("Snapshot storage is unavailable.");
        Mock<ISnapshotBlobRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.WriteAsync(snapshotKey, envelope, cancellation.Token)).ThrowsAsync(failure);
        Mock<ILogger<SnapshotBlobStorageProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        SnapshotBlobStorageProvider provider = new(repository.Object, logger.Object);
        IOException exception = await Assert.ThrowsAsync<IOException>(() =>
            provider.WriteAsync(snapshotKey, envelope, cancellation.Token));
        Assert.Same(failure, exception);
        MetricMeasurement count = Assert.Single(
            measurements.ToArray(),
            measurement => measurement.InstrumentName == "blob.snapshot.write.count");
        Assert.Equal(1, count.LongValue);
        Assert.Equal("failure", count.Tags["result"]);
        logger.Verify(
            value => value.Log(
                LogLevel.Error,
                It.Is<EventId>(eventId => eventId.Id == 12),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => state.ToString()!.Contains(snapshotKey.ToString(), StringComparison.Ordinal)),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        repository.Verify(value => value.WriteAsync(snapshotKey, envelope, cancellation.Token), Times.Once);
    }
}