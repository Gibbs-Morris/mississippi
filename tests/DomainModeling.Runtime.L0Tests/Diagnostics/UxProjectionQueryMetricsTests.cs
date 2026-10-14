using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Diagnostics;

using Moq;

using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Tests latest and exact-version query instrumentation; public for xUnit discovery.
/// </summary>
public sealed class UxProjectionQueryMetricsTests
{
    /// <summary>
    ///     Verifies one query measurement, exact routing, cancellation, and existing logging boundaries.
    /// </summary>
    /// <param name="version">The position selected for the query.</param>
    /// <param name="hasProjection">Whether the versioned cache returns state.</param>
    /// <param name="isLoggingEnabled">Whether debug logging is enabled.</param>
    /// <param name="readKind">The public operation to exercise.</param>
    /// <returns>An asynchronous test task.</returns>
    [Theory]
    [InlineData(0, true, true, "versioned")]
    [InlineData(42, true, true, "versioned")]
    [InlineData(42, false, true, "versioned")]
    [InlineData(-1, false, true, "versioned")]
    [InlineData(42, true, false, "versioned")]
    [InlineData(0, true, true, "latest")]
    [InlineData(42, true, true, "latest")]
    [InlineData(42, false, true, "latest")]
    [InlineData(-1, false, true, "latest")]
    [InlineData(42, true, false, "latest")]
    public async Task ReadRecordsOneQueryAndPreservesSelectedVersion(
        int version,
        bool hasProjection,
        bool isLoggingEnabled,
        string readKind
    )
    {
        // Arrange
        const string entityId = "query-metric-123";
        BrookPosition position = new(version);
        CancellationToken token = TestContext.Current.CancellationToken;
        VersionedQueryMetricProjection? projection = hasProjection ? new(version) : null;
        ConcurrentQueue<(string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags)>
            measurements = new();
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if ((instrument.Meter.Name == UxProjectionMetrics.MeterName) &&
                instrument.Name.StartsWith("projection.query.", StringComparison.Ordinal))
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            RecordMeasurement(instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            RecordMeasurement(instrument, value, tags));
        listener.Start();
        Mock<IGrainContext> contextMock = new();
        contextMock.Setup(c => c.GrainId).Returns(GrainId.Create("test", entityId));
        Mock<IUxProjectionGrainFactory> factoryMock = new(MockBehavior.Strict);
        Mock<IUxProjectionVersionedCacheGrain<VersionedQueryMetricProjection>> cacheMock = new(MockBehavior.Strict);
        cacheMock.Setup(c => c.GetAsync(token)).ReturnsAsync(projection);
        if (!position.NotSet)
        {
            UxProjectionVersionedCacheKey key = new("TEST.MODULE.QUERYMETRIC", entityId, position);
            factoryMock.Setup(f => f.GetUxProjectionVersionedCacheGrain<VersionedQueryMetricProjection>(key))
                .Returns(cacheMock.Object);
        }

        Mock<IUxProjectionCursorGrain> cursorMock = new(MockBehavior.Strict);
        if (readKind == "latest")
        {
            cursorMock.Setup(c => c.GetPositionAsync()).ReturnsAsync(position);
            factoryMock.Setup(f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()))
                .Returns(cursorMock.Object);
        }

        Mock<ILogger<UxProjectionGrain<VersionedQueryMetricProjection>>> loggerMock = new();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(isLoggingEnabled);
        UxProjectionGrain<VersionedQueryMetricProjection> grain = new(
            contextMock.Object,
            factoryMock.Object,
            loggerMock.Object);
        await grain.OnActivateAsync(token);

        // Act
        VersionedQueryMetricProjection? result = readKind switch
        {
            "latest" => await grain.GetAsync(token),
            "versioned" => await grain.GetAtVersionAsync(position, token),
            var _ => throw new ArgumentOutOfRangeException(nameof(readKind)),
        };

        // Assert
        Assert.Same(projection, result);
        cacheMock.Verify(c => c.GetAsync(token), position.NotSet ? Times.Never : Times.Once);
        factoryMock.Verify(
            f => f.GetUxProjectionCursorGrain(It.IsAny<UxProjectionCursorKey>()),
            readKind == "latest" ? Times.Once : Times.Never);
        cursorMock.Verify(c => c.GetPositionAsync(), readKind == "latest" ? Times.Once : Times.Never);
        factoryMock.Verify(
            f => f.GetUxProjectionVersionedCacheGrain<VersionedQueryMetricProjection>(
                It.Is<UxProjectionVersionedCacheKey>(key =>
                    (key.BrookName == "TEST.MODULE.QUERYMETRIC") &&
                    (key.EntityId == entityId) &&
                    (key.Version == position))),
            position.NotSet ? Times.Never : Times.Once);
        factoryMock.VerifyNoOtherCalls();
        (string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags)[] snapshot =
            measurements.ToArray();
        (string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags) count = Assert.Single(
            snapshot,
            m => m.Name == "projection.query.count");
        Assert.Equal(1, count.Value);
        Assert.Equal("queries", count.Unit);
        Assert.Equal(readKind, count.Tags["query.type"]);
        Assert.Equal(2, count.Tags.Count);
        (string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags) duration = Assert.Single(
            snapshot,
            m => m.Name == "projection.query.duration");
        Assert.True(duration.Value >= 0);
        Assert.Equal("ms", duration.Unit);
        Assert.Equal(readKind, duration.Tags["query.type"]);
        Assert.Equal(2, duration.Tags.Count);
        (string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags)[] empty =
            snapshot.Where(m => m.Name == "projection.query.empty").ToArray();
        if (hasProjection)
        {
            Assert.Empty(empty);
        }
        else
        {
            (string Name, string? Unit, double Value, IReadOnlyDictionary<string, object?> Tags) emptyMeasurement =
                Assert.Single(empty);
            Assert.Equal(1, emptyMeasurement.Value);
            Assert.Equal("queries", emptyMeasurement.Unit);
            Assert.Single(emptyMeasurement.Tags);
        }

        IInvocation[] delegationLogs = loggerMock.Invocations
            .Where(i => (i.Method.Name == "Log") && (((EventId)i.Arguments[1]).Id == 3))
            .ToArray();
        if (isLoggingEnabled && (readKind == "latest") && !position.NotSet)
        {
            IInvocation invocation = Assert.Single(delegationLogs);
            Assert.Equal(LogLevel.Debug, invocation.Arguments[0]);
            Assert.Equal("GetAsyncDelegatingToVersion", ((EventId)invocation.Arguments[1]).Name);
            IReadOnlyList<KeyValuePair<string, object?>> state =
                Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(invocation.Arguments[2], false);
            Assert.Contains(state, field => (field.Key == "EntityId") && Equals(field.Value, entityId));
            Assert.Contains(state, field => (field.Key == "Version") && Equals(field.Value, position));
        }
        else
        {
            Assert.Empty(delegationLogs);
        }

        return;

        void RecordMeasurement(
            Instrument instrument,
            double value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags
        )
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (tagMap.TryGetValue("projection.type", out object? projectionType) &&
                (projectionType as string == nameof(VersionedQueryMetricProjection)))
            {
                measurements.Enqueue((instrument.Name, instrument.Unit, value, tagMap));
            }
        }
    }
}