using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests;

/// <summary>
///     Tests for <see cref="BrookStorageProvider" /> behavior.
/// </summary>
public class BrookStorageProviderTests
{
    /// <summary>
    ///     Verifies constructor throws when the recovery service is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenRecoveryServiceIsNull()
    {
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        Assert.Throws<ArgumentNullException>(() => new BrookStorageProvider(null!, reader.Object, appender.Object));
    }

    /// <summary>
    ///     Verifies constructor throws when the event reader is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEventReaderIsNull()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        Assert.Throws<ArgumentNullException>(() => new BrookStorageProvider(recovery.Object, null!, appender.Object));
    }

    /// <summary>
    ///     Verifies constructor throws when the event appender is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEventAppenderIsNull()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Assert.Throws<ArgumentNullException>(() => new BrookStorageProvider(recovery.Object, reader.Object, null!));
    }

    /// <summary>
    ///     Verifies the storage provider reports the expected format string.
    /// </summary>
    [Fact]
    public void FormatReturnsCosmosDb()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        Assert.Equal("cosmos-db", provider.Format);
    }

    /// <summary>
    ///     Verifies head position reads delegate to the recovery service and return its value.
    /// </summary>
    [Fact]
    public async Task ReadHeadPositionAsyncDelegatesToRecoveryServiceAsync()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookKey brookId = new("type", "id");
        BrookPosition expected = new(42);
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        BrookPosition result = await provider.ReadHeadPositionAsync(brookId, CancellationToken.None);
        Assert.Equal(expected, result);
        recovery.Verify(r => r.GetOrRecoverHeadPositionAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        recovery.VerifyNoOtherCalls();
        reader.VerifyNoOtherCalls();
        appender.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies events are yielded directly from the reader.
    /// </summary>
    [Fact]
    public async Task ReadEventsAsyncYieldsFromReaderAsync()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookRangeKey range = new("type", "id", 0, 3);
        List<BrookEvent> events = new()
        {
            new()
            {
                Id = "e1",
                Source = "s",
                Type = "t",
                Data = ImmutableArray<byte>.Empty,
            },
            new()
            {
                Id = "e2",
                Source = "s",
                Type = "t",
                Data = ImmutableArray<byte>.Empty,
            },
            new()
            {
                Id = "e3",
                Source = "s",
                Type = "t",
                Data = ImmutableArray<byte>.Empty,
            },
        };
        reader.Setup(r => r.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(AsAsyncEnumerableAsync(events));
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        List<BrookEvent> collected = new();
        await foreach (BrookEvent e in provider.ReadEventsAsync(range, CancellationToken.None))
        {
            collected.Add(e);
        }

        Assert.Equal(3, collected.Count);
        Assert.Equal(events.Select(e => e.Id), collected.Select(e => e.Id));
        reader.Verify(r => r.ReadEventsAsync(range, It.IsAny<CancellationToken>()), Times.Once);
        recovery.VerifyNoOtherCalls();
        reader.VerifyNoOtherCalls();
        appender.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies appending with null events throws an argument exception.
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncThrowsWhenEventsNullAsync()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.AppendEventsAsync(new("t", "i"), null!, null, CancellationToken.None));
    }

    /// <summary>
    ///     Verifies appending with empty events throws an argument exception.
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncThrowsWhenEventsEmptyAsync()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        await Assert.ThrowsAsync<ArgumentException>(async () => await provider.AppendEventsAsync(
            new("t", "i"),
            Array.Empty<BrookEvent>(),
            null,
            CancellationToken.None));
    }

    /// <summary>
    ///     Verifies appending delegates to the appender and returns its result.
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncDelegatesToAppenderAndReturnsResultAsync()
    {
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<IEventBrookReader> reader = new(MockBehavior.Strict);
        Mock<IEventBrookAppender> appender = new(MockBehavior.Strict);
        BrookKey brookId = new("type", "id");
        BrookPosition expected = new(7);
        BrookEvent[] events = new[]
        {
            new BrookEvent
            {
                Id = "e1",
                Source = "s",
                Type = "t",
                Data = ImmutableArray<byte>.Empty,
            },
        };
        BrookPosition? expectedVersion = new BrookPosition(6);
        appender.Setup(a => a.AppendEventsAsync(
                brookId,
                It.Is<IReadOnlyList<BrookEvent>>(l => l.Count == 1),
                expectedVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        BrookStorageProvider provider = new(recovery.Object, reader.Object, appender.Object);
        BrookPosition result = await provider.AppendEventsAsync(
            brookId,
            events,
            expectedVersion,
            CancellationToken.None);
        Assert.Equal(expected, result);
        appender.Verify(
            a => a.AppendEventsAsync(
                brookId,
                It.Is<IReadOnlyList<BrookEvent>>(l => l.Count == 1),
                expectedVersion,
                It.IsAny<CancellationToken>()),
            Times.Once);
        recovery.VerifyNoOtherCalls();
        reader.VerifyNoOtherCalls();
        appender.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Helper that converts a sequence to an async-enumerable.
    /// </summary>
    /// <param name="items">The items to enumerate.</param>
    /// <returns>An async sequence of items.</returns>
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
}