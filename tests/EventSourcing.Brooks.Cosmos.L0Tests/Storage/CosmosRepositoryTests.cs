using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Storage;

/// <summary>
///     Tests for <see cref="CosmosRepository" /> covering the Storage/CosmosRepository plan items.
/// </summary>
public class CosmosRepositoryTests
{
    private static bool CaptureMaxItemCount(
        QueryRequestOptions options,
        out int count
    )
    {
        count = options.MaxItemCount ?? 0;
        return true;
    }

    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter = null
    )
    {
        Type type = typeof(CosmosException);
        ConstructorInfo[] ctors =
            type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? ctor =
            ctors.FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode)));
        if (ctor is null)
        {
            throw new InvalidOperationException("No suitable CosmosException constructor found for tests.");
        }

        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo p = parameters[i];
            if (p.ParameterType == typeof(string))
            {
                args[i] = string.Empty;
            }
            else if (p.ParameterType == typeof(HttpStatusCode))
            {
                args[i] = statusCode;
            }
            else if (p.ParameterType == typeof(int))
            {
                args[i] = 0;
            }
            else if (p.ParameterType == typeof(long))
            {
                args[i] = 0L;
            }
            else if (p.ParameterType == typeof(TimeSpan))
            {
                args[i] = retryAfter ?? TimeSpan.Zero;
            }
            else if (p.ParameterType.IsValueType)
            {
                args[i] = Activator.CreateInstance(p.ParameterType);
            }
            else
            {
                args[i] = null;
            }
        }

        CosmosException? instance = (CosmosException?)ctor.Invoke(args);
        if (instance is null)
        {
            throw new InvalidOperationException("Failed to construct CosmosException");
        }

        return instance;
    }

    private static CosmosRepository CreateRepository(
        Container container,
        IMapper<CursorDocument, CursorStorageModel>? cursorMapper = null,
        IMapper<EventDocument, EventStorageModel>? eventMapper = null
    )
    {
        cursorMapper ??= Mock.Of<IMapper<CursorDocument, CursorStorageModel>>(m => m.Map(It.IsAny<CursorDocument>()) ==
            new CursorStorageModel
            {
                Position = new(0),
            });
        eventMapper ??=
            Mock.Of<IMapper<EventDocument, EventStorageModel>>(m =>
                m.Map(It.IsAny<EventDocument>()) == new EventStorageModel());
        return new(container, new NoOpRetryPolicy(), cursorMapper, eventMapper);
    }

    /// <summary>
    ///     Minimal FeedIterator implementation for tests that yields provided pages.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    private sealed class FakeFeedIterator<T>
        : FeedIterator<T>,
          IDisposable
    {
        private readonly Queue<IReadOnlyList<T>> pages;

        public FakeFeedIterator(
            IEnumerable<List<T>> pages
        ) =>
            this.pages = new(pages);

        public override bool HasMoreResults => pages.Count > 0;

        public new void Dispose()
        {
            // nothing to dispose
        }

        public override Task<FeedResponse<T>> ReadNextAsync(
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<T> next = pages.Dequeue();
            Mock<FeedResponse<T>> response = new();

            // Defer enumerator creation to the moment the enumerator is consumed to avoid creating
            // an IDisposable during test setup which the analyzer flags (IDISP004).
            response.As<IEnumerable<T>>().Setup(r => r.GetEnumerator()).Returns(() => next.GetEnumerator());
            return Task.FromResult(response.Object);
        }
    }

    /// <summary>
    ///     Simple retry policy that performs the operation once without additional behavior.
    /// </summary>
    private sealed class NoOpRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     Verifies AppendEventBatchAsync appends events sequentially with correct mapping.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventBatchAsyncAppendsSequentiallyAsync()
    {
        // Arrange
        Mock<Container> container = new();
        List<EventDocument> created = new();
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback((
                EventDocument d,
                PartitionKey? pk,
                ItemRequestOptions? options,
                CancellationToken ct
            ) => created.Add(d))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        CosmosRepository sut = CreateRepository(container.Object);
        BrookKey key = new("type", "id");
        IReadOnlyList<EventStorageModel> events = new List<EventStorageModel>
        {
            new()
            {
                EventId = "e1",
                EventType = "A",
                Data = new byte[] { 1 },
                Time = DateTimeOffset.UtcNow,
            },
            new()
            {
                EventId = "e2",
                EventType = "B",
                Data = new byte[] { 2 },
                Time = DateTimeOffset.UtcNow,
            },
        };

        // Act
        await sut.AppendEventBatchAsync(key, events, 10);

        // Assert
        // Expect two created items; detailed assertions below verify each created document
        Assert.Equal(2, created.Count);
        Assert.Collection(
            created,
            d =>
            {
                Assert.Equal("10", d.Id);
                Assert.Equal(10, d.Position);
                Assert.Equal(key.ToString(), d.BrookPartitionKey);
                Assert.Equal("e1", d.EventId);
                Assert.Equal("A", d.EventType);
                Assert.Single(d.Data);
                Assert.Equal(1, d.Data[0]);
            },
            d =>
            {
                Assert.Equal("11", d.Id);
                Assert.Equal(11, d.Position);
                Assert.Equal(key.ToString(), d.BrookPartitionKey);
                Assert.Equal("e2", d.EventId);
                Assert.Equal("B", d.EventType);
                Assert.Single(d.Data);
                Assert.Equal(2, d.Data[0]);
            });
    }

    /// <summary>
    ///     Verifies CommitCursorPositionAsync upserts cursor and deletes pending cursor.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CommitCursorPositionAsyncUpsertsCursorAndDeletesPendingAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.UpsertItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<CursorDocument>>());
        container.Setup(c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<CursorDocument>>());
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        await sut.CommitCursorPositionAsync(new("type", "id"), 42);

        // Assert
        container.Verify(
            c => c.UpsertItemAsync(
                It.Is<CursorDocument>(h => (h.Id == "cursor") && (h.Position == 42)),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
        container.Verify(
            c => c.DeleteItemAsync<CursorDocument>(
                It.Is<string>(s => s == "cursor-pending"),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies CreatePendingCursorAsync creates the expected pending cursor document.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CreatePendingCursorAsyncCreatesPendingCursorAsync()
    {
        // Arrange
        Mock<Container> container = new();
        CursorDocument? captured = null;
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback((
                CursorDocument d,
                PartitionKey? pk,
                ItemRequestOptions? options,
                CancellationToken ct
            ) => captured = d)
            .ReturnsAsync(Mock.Of<ItemResponse<CursorDocument>>());
        CosmosRepository sut = CreateRepository(container.Object);
        BrookKey key = new("type", "id");

        // Act
        await sut.CreatePendingCursorAsync(key, new(5), 10);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("cursor-pending", captured!.Id);
        Assert.Equal("cursor-pending", captured.Type);
        Assert.Equal(10, captured.Position);
        Assert.Equal(5, captured.OriginalPosition);
        Assert.Equal(key.ToString(), captured.BrookPartitionKey);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies DeleteEventAsync ignores NotFound exceptions.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DeleteEventAsyncIgnoresNotFoundAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));
        CosmosRepository sut = CreateRepository(container.Object);

        // Act - should not throw
        Exception? ex = await Record.ExceptionAsync(() => sut.DeleteEventAsync(new("t", "i"), 9));

        // Assert - delete was attempted exactly once and exception swallowed
        Assert.Null(ex);
        container.Verify(
            c => c.DeleteItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies DeletePendingCursorAsync ignores NotFound exceptions.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task DeletePendingCursorAsyncIgnoresNotFoundAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));
        CosmosRepository sut = CreateRepository(container.Object);

        // Act - should not throw
        Exception? ex = await Record.ExceptionAsync(() => sut.DeletePendingCursorAsync(new("t", "i")));

        // Assert - delete was attempted exactly once and exception swallowed
        Assert.Null(ex);
        container.Verify(
            c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies EventExistsAsync returns false when Cosmos reports NotFound.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task EventExistsAsyncReturnsFalseOnNotFoundAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        bool exists = await sut.EventExistsAsync(new("type", "id"), 7);

        // Assert
        Assert.False(exists);
    }

    /// <summary>
    ///     Verifies EventExistsAsync returns true when ReadItemAsync succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task EventExistsAsyncReturnsTrueWhenExistsAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        bool exists = await sut.EventExistsAsync(new("type", "id"), 7);

        // Assert
        Assert.True(exists);
    }

    /// <summary>
    ///     Verifies ExecuteTransactionalBatchAsync returns non-success responses without retry when non-transient.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ExecuteTransactionalBatchAsyncReturnsNonTransientResponseAsync()
    {
        // Arrange
        Mock<Container> container = new();
        Mock<TransactionalBatch> batch = new();
        batch.Setup(b => b.ReplaceItem(It.IsAny<string>(), It.IsAny<CursorDocument>(), null)).Returns(batch.Object);
        Mock<TransactionalBatchResponse> nonTransient = new();
        nonTransient.SetupGet(r => r.IsSuccessStatusCode).Returns(false);
        nonTransient.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);
        batch.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(nonTransient.Object);
        container.Setup(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>())).Returns(batch.Object);
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        using TransactionalBatchResponse resp = await sut.ExecuteTransactionalBatchAsync(
            new("t", "i"),
            Array.Empty<EventStorageModel>(),
            new(0),
            1);

        // Assert
        Assert.Same(nonTransient.Object, resp);
    }

    /// <summary>
    ///     Verifies ExecuteTransactionalBatchAsync surfaces 413 (RequestEntityTooLarge) as InvalidOperationException.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ExecuteTransactionalBatchAsyncThrowsOnRequestEntityTooLargeAsync()
    {
        // Arrange
        Mock<Container> container = new();
        Mock<TransactionalBatch> batch = new();
        batch.Setup(b => b.ReplaceItem(It.IsAny<string>(), It.IsAny<CursorDocument>(), null)).Returns(batch.Object);

        // Simulate CosmosException 413 thrown from ExecuteAsync
        CosmosException ex = CreateCosmosException(HttpStatusCode.RequestEntityTooLarge);
        batch.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ThrowsAsync(ex);
        container.Setup(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>())).Returns(batch.Object);
        CosmosRepository sut = CreateRepository(container.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using TransactionalBatchResponse disposedResponse = await sut.ExecuteTransactionalBatchAsync(
                new("t", "i"),
                Array.Empty<EventStorageModel>(),
                new(0),
                1);
        });
    }

    /// <summary>
    ///     Verifies ExecuteTransactionalBatchAsync replaces the cursor and retries on transient status until success.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ExecuteTransactionalBatchAsyncUpsertsCursorWithRetriesAsync()
    {
        // Arrange
        Mock<Container> container = new();
        Mock<TransactionalBatch> batch = new();

        // Chain ReplaceItem; CreateItem should not be called in this scenario
        batch.Setup(b => b.ReplaceItem(It.Is<string>(id => id == "cursor"), It.IsAny<CursorDocument>(), null))
            .Returns(batch.Object);
        batch.Setup(b => b.CreateItem(It.IsAny<CursorDocument>(), null)).Returns(batch.Object);

        // First response 429 with RetryAfter, second success
        Mock<TransactionalBatchResponse> r1 = new();
        r1.SetupGet(r => r.IsSuccessStatusCode).Returns(false);
        r1.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.TooManyRequests);
        r1.SetupGet(r => r.RetryAfter).Returns(TimeSpan.FromMilliseconds(1));
        Mock<TransactionalBatchResponse> r2 = new();
        r2.SetupGet(r => r.IsSuccessStatusCode).Returns(true);
        r2.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);
        int calls = 0;
        batch.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                return calls == 1 ? r1.Object : r2.Object;
            });
        container.Setup(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>())).Returns(batch.Object);
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        using TransactionalBatchResponse response = await sut.ExecuteTransactionalBatchAsync(
            new("t", "i"),
            Array.Empty<EventStorageModel>(),
            new(1),
            2);

        // Assert
        Assert.Same(r2.Object, response);
        batch.Verify(b => b.ReplaceItem("cursor", It.IsAny<CursorDocument>(), null), Times.Once);
        batch.Verify(b => b.CreateItem(It.IsAny<CursorDocument>(), null), Times.Never);
        batch.Verify(b => b.ExecuteAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    /// <summary>
    ///     Verifies GetCursorDocumentAsync returns null when Cosmos returns NotFound.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task GetCursorDocumentAsyncReturnsNullOnNotFoundAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        CursorStorageModel? result = await sut.GetCursorDocumentAsync(new("type", "id"));

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Verifies GetExistingEventPositionsAsync returns the set of positions from the iterator.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task GetExistingEventPositionsAsyncReturnsPositionsAsync()
    {
        // Arrange
        Mock<Container> container = new();

        // Two pages: [1,3] then [5]
        using FakeFeedIterator<long> iterator = new(
            new List<List<long>>
            {
                new()
                {
                    1,
                    3,
                },
                new()
                {
                    5,
                },
            });
        container.Setup(c => c.GetItemQueryIterator<long>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Returns(iterator);
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        ISet<long> result = await sut.GetExistingEventPositionsAsync(new("t", "i"), 1, 6);

        // Assert
        Assert.Equal(
            new HashSet<long>
            {
                1,
                3,
                5,
            },
            result);
    }

    /// <summary>
    ///     Verifies GetPendingCursorDocumentAsync returns null when Cosmos returns NotFound.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task GetPendingCursorDocumentAsyncReturnsNullOnNotFoundAsync()
    {
        // Arrange
        Mock<Container> container = new();
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateCosmosException(HttpStatusCode.NotFound));
        CosmosRepository sut = CreateRepository(container.Object);

        // Act
        CursorStorageModel? result = await sut.GetPendingCursorDocumentAsync(new("type", "id"));

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Verifies QueryEventsAsync respects batch size and cancellation.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task QueryEventsAsyncRespectsBatchSizeAndCancellationAsync()
    {
        // Arrange
        Mock<Container> container = new();
        int capturedBatchSize = 0;
        using FakeFeedIterator<EventDocument> iterator2 = new(
            new List<List<EventDocument>>
            {
                new()
                {
                    new(),
                },
            });
        container.Setup(c => c.GetItemQueryIterator<EventDocument>(
                It.IsAny<QueryDefinition>(),
                null,
                It.Is<QueryRequestOptions>(o => CaptureMaxItemCount(o, out capturedBatchSize))))
            .Returns(iterator2);
        CosmosRepository sut = CreateRepository(container.Object);
        BrookRangeKey range = new("t", "i", 0, 10);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (EventStorageModel m in sut.QueryEventsAsync(range, 5, cts.Token))
            {
                // Enumerate to ensure cancellation is observed while ignoring the element.
                _ = m;
            }
        });
        Assert.Equal(5, capturedBatchSize);
    }

    /// <summary>
    ///     Verifies QueryEventsAsync maps and yields models in-order across multiple pages.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task QueryEventsAsyncReturnsMappedModelsInOrderAsync()
    {
        // Arrange
        Mock<Container> container = new();

        // Two pages
        List<EventDocument> p1 = new()
        {
            new()
            {
                EventId = "e1",
                EventType = "A",
            },
            new()
            {
                EventId = "e2",
                EventType = "B",
            },
        };
        List<EventDocument> p2 = new()
        {
            new()
            {
                EventId = "e3",
                EventType = "C",
            },
        };
        using FakeFeedIterator<EventDocument> iterator = new(
            new List<List<EventDocument>>
            {
                p1,
                p2,
            });
        container.Setup(c => c.GetItemQueryIterator<EventDocument>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Returns(iterator);

        // Use a mapper that copies over EventId so we can assert ordering
        Mock<IMapper<EventDocument, EventStorageModel>> eventMapper = new();
        eventMapper.Setup(m => m.Map(It.IsAny<EventDocument>()))
            .Returns<EventDocument>(d => new()
            {
                EventId = d.EventId,
                EventType = d.EventType,
            });
        CosmosRepository sut = CreateRepository(container.Object, eventMapper: eventMapper.Object);
        BrookRangeKey range = new("t", "i", 0, 100);

        // Act
        List<EventStorageModel> results = new();
        await foreach (EventStorageModel m in sut.QueryEventsAsync(range, 2))
        {
            results.Add(m);
        }

        // Assert
        Assert.Collection(
            results.Select(r => r.EventId),
            id => Assert.Equal("e1", id),
            id => Assert.Equal("e2", id),
            id => Assert.Equal("e3", id));
    }
}