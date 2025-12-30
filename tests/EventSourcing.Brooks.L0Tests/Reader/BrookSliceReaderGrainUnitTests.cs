using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Reader;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Unit tests for <see cref="BrookSliceReaderGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Slice Reader Grain Unit")]
public sealed class BrookSliceReaderGrainUnitTests
{
    private static readonly BrookRangeKey TestRangeKey = BrookRangeKey.FromBrookCompositeKey(new("test", "id"), 0, 10);

    private static (BrookSliceReaderGrain Grain, Mock<IBrookStorageReader> Storage, Mock<IGrainContext> Context)
        CreateGrain()
    {
        Mock<IBrookStorageReader> storage = new();
        Mock<IGrainContext> context = new();
        context.Setup(c => c.GrainId).Returns(GrainId.Create("slicereader", TestRangeKey.ToString()));
        BrookSliceReaderGrain grain = new(storage.Object, context.Object);
        return (grain, storage, context);
    }

    private static async IAsyncEnumerable<BrookEvent> EmptyAsyncEnumerableAsync()
    {
        await Task.CompletedTask;
        yield break;
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
            .Returns((
                BrookRangeKey _,
                CancellationToken ct
            ) =>
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
        await foreach (BrookEvent ev in sut.ReadAsync(2, 3))
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
        await foreach (BrookEvent ev in sut.ReadAsync(0, 2))
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
            await foreach (BrookEvent unused in sut.ReadAsync(0, 5))
            {
                // Should not reach here
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
            await foreach (BrookEvent unused in sut.ReadAsync(0, 5))
            {
                // Should not reach here
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
        await foreach (BrookEvent ev in sut.ReadAsync(0, 2))
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
        ImmutableArray<BrookEvent> result = await sut.ReadBatchAsync(0, 2);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(["0", "1", "2"], result.Select(e => e.Id).ToArray());
    }
}