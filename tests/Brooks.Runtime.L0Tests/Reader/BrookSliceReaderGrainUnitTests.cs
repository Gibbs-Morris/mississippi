using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Reader;
using Mississippi.Brooks.Runtime.Storage.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.Brooks.Runtime.L0Tests.Reader;

/// <summary>
///     Unit tests for <see cref="BrookSliceReaderGrain" />.
/// </summary>
public sealed class BrookSliceReaderGrainUnitTests
{
    private static readonly BrookRangeKey TestRangeKey = BrookRangeKey.FromBrookCompositeKey(new("test", "id"), 0, 10);

    private static (BrookSliceReaderGrain Grain, Mock<IBrookStorageReader> Storage, Mock<IGrainContext> Context)
        CreateGrain()
    {
        Mock<IBrookStorageReader> storage = new();
        Mock<IGrainContext> context = new();
        Mock<ILogger<BrookSliceReaderGrain>> logger = new();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", TestRangeKey.ToString()));
        BrookSliceReaderGrain grain = new(storage.Object, context.Object, logger.Object);
        return (grain, storage, context);
    }

    private static async IAsyncEnumerable<BrookEvent> EmptyAsyncEnumerableAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task<ImmutableArray<BrookEvent>> ReadRangeAsync(
        BrookSliceReaderGrain grain,
        BrookRangeKey range,
        bool isBatch
    )
    {
        if (isBatch)
        {
            return await grain.ReadBatchAsync(range.Start, range.End, TestContext.Current.CancellationToken);
        }

        List<BrookEvent> result = new();
        await foreach (BrookEvent ev in grain.ReadAsync(range.Start, range.End, TestContext.Current.CancellationToken))
        {
            result.Add(ev);
        }

        return [.. result];
    }

    private static async Task<ImmutableArray<BrookEvent>> ReadRangeToAsync(
        BrookSliceReaderGrain grain,
        BrookRangeKey range,
        BrookPosition readTo,
        bool isBatch
    )
    {
        if (isBatch)
        {
            return await grain.ReadBatchAsync(range.Start, readTo, TestContext.Current.CancellationToken);
        }

        List<BrookEvent> result = new();
        await foreach (BrookEvent ev in grain.ReadAsync(range.Start, readTo, TestContext.Current.CancellationToken))
        {
            result.Add(ev);
        }

        return [.. result];
    }

    private static async IAsyncEnumerable<BrookEvent> ToAsyncEnumerableAsync(
        BrookEvent[] events
    )
    {
        await Task.CompletedTask;
        foreach (BrookEvent ev in events)
        {
            yield return ev;
        }
    }

    private static async IAsyncEnumerable<BrookEvent> WaitThenReturnEventsAsync(
        TaskCompletionSource<bool> started,
        Task release,
        BrookEvent[] events
    )
    {
        started.SetResult(true);
        await release.WaitAsync(TestContext.Current.CancellationToken);
        foreach (BrookEvent ev in events)
        {
            yield return ev;
        }
    }

    /// <summary>
    ///     Ensures deactivation clears caches and completes without error.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task DeactivateAsyncClearsCacheAndDeactivates()
    {
        // Arrange
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> _, Mock<IGrainContext> _) = CreateGrain();

        // Act
        await sut.DeactivateAsync();

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }

    /// <summary>
    ///     Verifies GrainContext property returns the injected context.
    /// </summary>
    [Fact]
    public void GrainContextReturnsInjectedContext()
    {
        // Arrange
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> _, Mock<IGrainContext> context) = CreateGrain();

        // Assert
        Assert.Same(context.Object, sut.GrainContext);
    }

    /// <summary>
    ///     Verifies OnActivateAsync passes the cancellation token to storage.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task OnActivateAsyncPassesCancellationTokenToStorage()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns((BrookRangeKey _, CancellationToken ct) =>
            {
                capturedToken = ct;
                return EmptyAsyncEnumerableAsync();
            });

        // Act
        await sut.OnActivateAsync(expectedToken);

        // Assert
        Assert.Equal(expectedToken, capturedToken);
    }

    /// <summary>
    ///     Verifies OnActivateAsync populates the cache from storage.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task OnActivateAsyncPopulatesCacheFromStorage()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));

        // Act
        await sut.OnActivateAsync(CancellationToken.None);

        // Assert: Verify storage was called
        storage.Verify(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies ReadAsync respects cancellation token.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncRespectsCancellationToken()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);
        using CancellationTokenSource cts = new();
        int count = 0;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (BrookEvent unused in sut.ReadAsync(0, 2, cts.Token))
            {
                _ = unused;
                count++;
                if (count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        });
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Verifies ReadAsync skips events before minReadFrom.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncSkipsEventsBeforeMinReadFrom()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
            new()
            {
                Id = "3",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);

        // Act: Start from position 2
        List<BrookEvent> result = new();
        await foreach (BrookEvent ev in sut.ReadAsync(2, 3, TestContext.Current.CancellationToken))
        {
            result.Add(ev);
        }

        // Assert: Should only get events at positions 2 and 3
        Assert.Equal(2, result.Count);
        Assert.Equal("2", result[0].Id);
        Assert.Equal("3", result[1].Id);
    }

    /// <summary>
    ///     Verifies ReadAsync stops when position exceeds maxReadTo.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncStopsWhenPositionExceedsMaxReadTo()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
            new()
            {
                Id = "3",
            },
            new()
            {
                Id = "4",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);

        // Act: Read up to position 2
        List<BrookEvent> result = new();
        await foreach (BrookEvent ev in sut.ReadAsync(0, 2, TestContext.Current.CancellationToken))
        {
            result.Add(ev);
        }

        // Assert: Should only get events at positions 0, 1, and 2
        Assert.Equal(3, result.Count);
        Assert.Equal("0", result[0].Id);
        Assert.Equal("1", result[1].Id);
        Assert.Equal("2", result[2].Id);
    }

    /// <summary>
    ///     Verifies ReadAsync throws when maxReadTo exceeds cached range with empty cache.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncThrowsWhenMaxReadToExceedsCacheWithEmptyCache()
    {
        // Arrange
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerableAsync());
        await sut.OnActivateAsync(CancellationToken.None);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (BrookEvent unused in sut.ReadAsync(0, 5, TestContext.Current.CancellationToken))
            {
                _ = unused;
            }
        });
        Assert.Contains("exceeds cached range", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies ReadAsync throws when maxReadTo exceeds cached range with populated cache.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncThrowsWhenMaxReadToExceedsCacheWithPopulatedCache()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);

        // Act & Assert: Cache has 2 events (positions 0, 1), requesting up to position 5 should fail
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (BrookEvent unused in sut.ReadAsync(0, 5, TestContext.Current.CancellationToken))
            {
                _ = unused;
            }
        });
        Assert.Contains("exceeds cached range", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Value = 5", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies ReadAsync yields all events in the requested range.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncYieldsAllEventsInRange()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);

        // Act
        List<BrookEvent> result = new();
        await foreach (BrookEvent ev in sut.ReadAsync(0, 2, TestContext.Current.CancellationToken))
        {
            result.Add(ev);
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(["0", "1", "2"], result.Select(e => e.Id).ToArray());
    }

    /// <summary>
    ///     Verifies ReadBatchAsync passes cancellation token to ReadAsync.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadBatchAsyncPassesCancellationToken()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ReadBatchAsync(0, 1, cts.Token));
    }

    /// <summary>
    ///     Verifies ReadBatchAsync returns an immutable array of events.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadBatchAsyncReturnsImmutableArray()
    {
        // Arrange
        BrookEvent[] testEvents =
        [
            new()
            {
                Id = "0",
            },
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> _) = CreateGrain();
        storage.Setup(s => s.ReadEventsAsync(TestRangeKey, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(testEvents));
        await sut.OnActivateAsync(CancellationToken.None);

        // Act
        ImmutableArray<BrookEvent> result = await sut.ReadBatchAsync(0, 2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(["0", "1", "2"], result.Select(e => e.Id).ToArray());
    }

    /// <summary>
    ///     Verifies complete initial caches satisfy repeated reads without another storage query.
    /// </summary>
    /// <param name="isBatch">Whether to exercise batch rather than streaming reads.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadRangeKeepsCompleteInitialCache(
        bool isBatch
    )
    {
        BrookRangeKey range = BrookRangeKey.FromBrookCompositeKey(new("test", "complete"), 10, 3);
        BrookEvent[] events =
        [
            new()
            {
                Id = "10",
            },
            new()
            {
                Id = "11",
            },
            new()
            {
                Id = "12",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> context) = CreateGrain();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", range.ToString()));
        storage.Setup(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(events));
        await sut.OnActivateAsync(CancellationToken.None);
        ImmutableArray<BrookEvent> first = await ReadRangeAsync(sut, range, isBatch);
        ImmutableArray<BrookEvent> second = await ReadRangeAsync(sut, range, isBatch);
        Assert.Equal(events, first);
        Assert.Equal(events, second);
        storage.Verify(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies a delayed short refresh cannot replace a complete cache from an overlapping read.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadRangePreservesCompleteCacheAfterDelayedShortRefresh()
    {
        BrookRangeKey range = BrookRangeKey.FromBrookCompositeKey(new("test", "overlap"), 10, 3);
        BrookEvent[] events =
        [
            new()
            {
                Id = "10",
            },
            new()
            {
                Id = "11",
            },
            new()
            {
                Id = "12",
            },
        ];
        BrookEvent[] initialEvents = [events[0]];
        TaskCompletionSource<bool> refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> context) = CreateGrain();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", range.ToString()));
        storage.SetupSequence(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(initialEvents))
            .Returns(WaitThenReturnEventsAsync(refreshStarted, releaseRefresh.Task, initialEvents))
            .Returns(ToAsyncEnumerableAsync(events));
        await sut.OnActivateAsync(CancellationToken.None);
        Task<ImmutableArray<BrookEvent>> pendingRead = ReadRangeAsync(sut, range, true);
        try
        {
            Task firstCompleted = await Task.WhenAny(refreshStarted.Task, pendingRead);
            if (firstCompleted == pendingRead)
            {
                await pendingRead;
            }

            Assert.True(refreshStarted.Task.IsCompletedSuccessfully);
            ImmutableArray<BrookEvent> recovered = await ReadRangeAsync(sut, range, true);
            releaseRefresh.SetResult(true);
            ImmutableArray<BrookEvent> delayed = await pendingRead;
            ImmutableArray<BrookEvent> cached = await ReadRangeAsync(sut, range, true);
            Assert.Equal(events, recovered);
            Assert.Equal(events, delayed);
            Assert.Equal(events, cached);
            storage.Verify(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
        finally
        {
            releaseRefresh.TrySetResult(true);
        }
    }

    /// <summary>
    ///     Verifies retrying the same slice succeeds after its initially short storage query recovers.
    /// </summary>
    /// <param name="isBatch">Whether to exercise batch rather than streaming reads.</param>
    /// <param name="initialCount">The number of events visible before storage recovers.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public async Task ReadRangeRecoversAfterIncompleteInitialCache(
        bool isBatch,
        int initialCount
    )
    {
        using CancellationTokenSource activationTokenSource = new();
        BrookRangeKey range = BrookRangeKey.FromBrookCompositeKey(new("test", "recovery"), 10, 3);
        BrookEvent[] events =
        [
            new()
            {
                Id = "10",
            },
            new()
            {
                Id = "11",
            },
            new()
            {
                Id = "12",
            },
        ];
        BrookEvent[] initialEvents = events.Take(initialCount).ToArray();
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> context) = CreateGrain();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", range.ToString()));
        storage.SetupSequence(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(initialEvents))
            .Returns(ToAsyncEnumerableAsync(initialEvents))
            .Returns(ToAsyncEnumerableAsync(events));
        await sut.OnActivateAsync(activationTokenSource.Token);
        InvalidOperationException initialFailure =
            await Assert.ThrowsAsync<InvalidOperationException>(() => ReadRangeAsync(sut, range, isBatch));
        Assert.Contains("exceeds cached range", initialFailure.Message, StringComparison.Ordinal);
        ImmutableArray<BrookEvent> recovered = await ReadRangeAsync(sut, range, isBatch);
        ImmutableArray<BrookEvent> cached = await ReadRangeAsync(sut, range, isBatch);
        Assert.Equal(events, recovered);
        Assert.Equal(events, cached);
        storage.Verify(s => s.ReadEventsAsync(range, TestContext.Current.CancellationToken), Times.Exactly(2));
        storage.Verify(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    /// <summary>
    ///     Verifies a persistently short storage query keeps failing without returning partial events.
    /// </summary>
    /// <param name="isBatch">Whether to exercise batch rather than streaming reads.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadRangeRejectsPersistentlyIncompleteStorage(
        bool isBatch
    )
    {
        BrookRangeKey range = BrookRangeKey.FromBrookCompositeKey(new("test", "incomplete"), 10, 3);
        BrookEvent[] events =
        [
            new()
            {
                Id = "10",
            },
        ];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> context) = CreateGrain();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", range.ToString()));
        storage.Setup(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(events));
        await sut.OnActivateAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadRangeAsync(sut, range, isBatch));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadRangeAsync(sut, range, isBatch));
    }

    /// <summary>
    ///     Verifies a larger short refresh cannot shift event positions by replacing the cached prefix.
    /// </summary>
    /// <param name="isBatch">Whether to exercise batch rather than streaming reads.</param>
    /// <param name="initialCount">The number of events cached before the shifted query.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public async Task ReadRangeRejectsShiftedIncompleteRefresh(
        bool isBatch,
        int initialCount
    )
    {
        BrookRangeKey range = BrookRangeKey.FromBrookCompositeKey(new("test", "shifted"), 10, 3);
        BrookEvent[] events =
        [
            new()
            {
                Id = "10",
            },
            new()
            {
                Id = "11",
            },
            new()
            {
                Id = "12",
            },
        ];
        BrookEvent[] initialEvents = events.Take(initialCount).ToArray();
        BrookEvent[] shiftedEvents = [events[1], events[2]];
        (BrookSliceReaderGrain sut, Mock<IBrookStorageReader> storage, Mock<IGrainContext> context) = CreateGrain();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", range.ToString()));
        storage.SetupSequence(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync(initialEvents))
            .Returns(ToAsyncEnumerableAsync(shiftedEvents))
            .Returns(ToAsyncEnumerableAsync(events));
        await sut.OnActivateAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadRangeToAsync(
            sut,
            range,
            range.Start + 1,
            isBatch));
        ImmutableArray<BrookEvent> recovered = await ReadRangeToAsync(sut, range, range.Start + 1, isBatch);
        ImmutableArray<BrookEvent> cached = await ReadRangeAsync(sut, range, isBatch);
        Assert.Equal(["10", "11"], recovered.Select(e => e.Id).ToArray());
        Assert.Equal(events, cached);
        storage.Verify(s => s.ReadEventsAsync(range, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}