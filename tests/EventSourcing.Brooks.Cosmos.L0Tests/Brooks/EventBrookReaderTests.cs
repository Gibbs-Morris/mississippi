using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos.Retry;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Brooks;

/// <summary>
///     Test class for EventBrookReader functionality.
///     Contains unit tests to verify the behavior of event brook reader implementations.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Cosmos")]
[AllureSubSuite("Event Brook Reader")]
public sealed class EventBrookReaderTests
{
    /// <summary>
    ///     Verifies that the <see cref="EventBrookReader" /> constructor throws when <c>options</c> is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenOptionsIsNull()
    {
        ICosmosRepository repository = new Mock<ICosmosRepository>(MockBehavior.Strict).Object;
        IRetryPolicy retryPolicy = new Mock<IRetryPolicy>(MockBehavior.Strict).Object;
        IMapper<EventStorageModel, BrookEvent> mapper =
            new Mock<IMapper<EventStorageModel, BrookEvent>>(MockBehavior.Strict).Object;
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new EventBrookReader(repository, retryPolicy, null!, mapper, NullLogger<EventBrookReader>.Instance));
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>
    ///     Verifies that ReadEventsAsync maps storage models to BrookEvent and yields them in order.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task ReadEventsAsyncMapsAndYieldsInOrderAsync()
    {
        // Arrange
        Mock<ICosmosRepository> repositoryMock = new(MockBehavior.Strict);
        IRetryPolicy retryPolicy = new Mock<IRetryPolicy>(MockBehavior.Strict).Object;
        Mock<IMapper<EventStorageModel, BrookEvent>> mapperMock = new(MockBehavior.Strict);
        BrookStorageOptions options = new()
        {
            QueryBatchSize = 2,
        };
        BrookRangeKey range = new("type", "id", 0, 3);
        EventStorageModel e1 = new()
        {
            EventId = "e1",
            Source = "src",
            EventType = "T1",
            DataContentType = "application/octet-stream",
            Data = Array.Empty<byte>(),
            Time = DateTimeOffset.UtcNow,
        };
        EventStorageModel e2 = new()
        {
            EventId = "e2",
            Source = "src",
            EventType = "T2",
            DataContentType = "application/octet-stream",
            Data = Array.Empty<byte>(),
            Time = DateTimeOffset.UtcNow,
        };
        EventStorageModel e3 = new()
        {
            EventId = "e3",
            Source = "src",
            EventType = "T3",
            DataContentType = "application/octet-stream",
            Data = Array.Empty<byte>(),
            Time = DateTimeOffset.UtcNow,
        };

        static async IAsyncEnumerable<EventStorageModel> SequenceAsync(
            params EventStorageModel[] items
        )
        {
            foreach (EventStorageModel item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }

        repositoryMock.Setup(r => r.QueryEventsAsync(range, options.QueryBatchSize, It.IsAny<CancellationToken>()))
            .Returns(SequenceAsync(e1, e2, e3));
        mapperMock.Setup(m => m.Map(e1))
            .Returns(
                new BrookEvent
                {
                    Id = e1.EventId,
                    Source = e1.Source ?? string.Empty,
                    EventType = e1.EventType,
                    DataContentType = e1.DataContentType ?? string.Empty,
                    Time = e1.Time,
                });
        mapperMock.Setup(m => m.Map(e2))
            .Returns(
                new BrookEvent
                {
                    Id = e2.EventId,
                    Source = e2.Source ?? string.Empty,
                    EventType = e2.EventType,
                    DataContentType = e2.DataContentType ?? string.Empty,
                    Time = e2.Time,
                });
        mapperMock.Setup(m => m.Map(e3))
            .Returns(
                new BrookEvent
                {
                    Id = e3.EventId,
                    Source = e3.Source ?? string.Empty,
                    EventType = e3.EventType,
                    DataContentType = e3.DataContentType ?? string.Empty,
                    Time = e3.Time,
                });
        EventBrookReader reader = new(
            repositoryMock.Object,
            retryPolicy,
            Options.Create(options),
            mapperMock.Object,
            NullLogger<EventBrookReader>.Instance);
        List<BrookEvent> results = new();

        // Act
        await foreach (BrookEvent ev in reader.ReadEventsAsync(range))
        {
            results.Add(ev);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Collection(
            results,
            a => Assert.Equal("T1", a.EventType),
            b => Assert.Equal("T2", b.EventType),
            c => Assert.Equal("T3", c.EventType));
        repositoryMock.Verify(
            r => r.QueryEventsAsync(range, options.QueryBatchSize, It.IsAny<CancellationToken>()),
            Times.Once);
        mapperMock.VerifyAll();
    }

    /// <summary>
    ///     Verifies that ReadEventsAsync respects the provided cancellation token and stops enumeration.
    /// </summary>
    /// <returns>A task that completes when the test finishes.</returns>
    [Fact]
    public async Task ReadEventsAsyncRespectsCancellationAsync()
    {
        // Arrange
        Mock<ICosmosRepository> repositoryMock = new(MockBehavior.Strict);
        IRetryPolicy retryPolicy = new Mock<IRetryPolicy>(MockBehavior.Strict).Object;
        Mock<IMapper<EventStorageModel, BrookEvent>> mapperMock = new(MockBehavior.Strict);
        BrookStorageOptions options = new()
        {
            QueryBatchSize = 10,
        };
        BrookRangeKey range = new("type", "id", 0, 100);
        EventStorageModel e1 = new()
        {
            EventId = "e1",
            EventType = "T1",
        };
        EventStorageModel e2 = new()
        {
            EventId = "e2",
            EventType = "T2",
        };

        static async IAsyncEnumerable<EventStorageModel> SequenceUntilCancelledAsync(
            EventStorageModel first,
            EventStorageModel second,
            [EnumeratorCancellation] CancellationToken token
        )
        {
            yield return first;
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            yield return second; // should not be reached if token is cancelled by the test
        }

        repositoryMock.Setup(r => r.QueryEventsAsync(range, options.QueryBatchSize, It.IsAny<CancellationToken>()))
            .Returns((
                BrookRangeKey _,
                int _,
                CancellationToken t
            ) => SequenceUntilCancelledAsync(e1, e2, t));
        mapperMock.Setup(m => m.Map(e1))
            .Returns(
                new BrookEvent
                {
                    Id = e1.EventId,
                    EventType = e1.EventType,
                });
        mapperMock.Setup(m => m.Map(e2))
            .Returns(
                new BrookEvent
                {
                    Id = e2.EventId,
                    EventType = e2.EventType,
                });
        EventBrookReader reader = new(
            repositoryMock.Object,
            retryPolicy,
            Options.Create(options),
            mapperMock.Object,
            NullLogger<EventBrookReader>.Instance);
        using CancellationTokenSource cts = new();
        List<BrookEvent> results = new();

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (BrookEvent ev in reader.ReadEventsAsync(range, cts.Token))
            {
                results.Add(ev);
                if (results.Count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        });
        Assert.Single(results);
        Assert.Equal("T1", results[0].EventType);
        repositoryMock.Verify(
            r => r.QueryEventsAsync(range, options.QueryBatchSize, It.IsAny<CancellationToken>()),
            Times.Once);
        mapperMock.Verify(m => m.Map(e1), Times.Once);

        // Mapper for e2 should not be invoked due to cancellation
        mapperMock.Verify(m => m.Map(e2), Times.Never);
    }
}