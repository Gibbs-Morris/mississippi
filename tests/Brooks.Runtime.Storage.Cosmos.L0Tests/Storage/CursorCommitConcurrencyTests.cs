using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Storage;

/// <summary>
///     Verifies that mixed-age cursor reads cannot regress committed history.
/// </summary>
public sealed class CursorCommitConcurrencyTests
{
    /// <summary>
    ///     Uses the cursor version and pending attempt in the same atomic commit.
    /// </summary>
    /// <param name="isMissingSnapshot">Whether the read reports that the cursor is absent.</param>
    /// <param name="hasAdvancedCursor">Whether storage has advanced beyond the read snapshot.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CursorCommitRejectsNewerHistoryAsync(
        bool isMissingSnapshot,
        bool hasAdvancedCursor
    )
    {
        BrookKey key = new("test", "cursor-version");
        long? actualCursor = isMissingSnapshot ? null : 1;
        if (hasAdvancedCursor)
        {
            actualCursor = 10;
        }

        string actualETag = hasAdvancedCursor ? "newer-cursor" : "observed-cursor";
        bool hasPending = true;
        string? operation = null;
        string? cursorCondition = null;
        string? pendingCondition = null;
        CursorDocument? candidate = null;
        Mock<Container> container = new(MockBehavior.Strict);
        if (isMissingSnapshot)
        {
            container.Setup(c => c.ReadItemAsync<CursorDocument>(
                    "cursor",
                    new(key.ToString()),
                    null,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Absent cursor snapshot", HttpStatusCode.NotFound, 0, "test", 0));
        }
        else
        {
            Mock<ItemResponse<CursorDocument>> cursorResponse = new();
            cursorResponse.SetupGet(r => r.Resource)
                .Returns(
                    new CursorDocument
                    {
                        Position = 1,
                    });
            cursorResponse.SetupGet(r => r.ETag).Returns("observed-cursor");
            container.Setup(c => c.ReadItemAsync<CursorDocument>(
                    "cursor",
                    new(key.ToString()),
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorResponse.Object);
        }

        Mock<TransactionalBatch> batch = new(MockBehavior.Strict);
        container.Setup(c => c.CreateTransactionalBatch(new(key.ToString()))).Returns(batch.Object);
        batch.Setup(b => b.DeleteItem("cursor-pending", It.IsAny<TransactionalBatchItemRequestOptions>()))
            .Callback<string, TransactionalBatchItemRequestOptions>((_, options) =>
                pendingCondition = options.IfMatchEtag)
            .Returns(batch.Object);
        batch.Setup(b => b.CreateItem(It.IsAny<CursorDocument>(), null))
            .Callback<CursorDocument, TransactionalBatchItemRequestOptions>((document, _) =>
            {
                operation = "create";
                candidate = document;
            })
            .Returns(batch.Object);
        batch.Setup(b => b.ReplaceItem(
                "cursor",
                It.IsAny<CursorDocument>(),
                It.IsAny<TransactionalBatchItemRequestOptions>()))
            .Callback<string, CursorDocument, TransactionalBatchItemRequestOptions>((_, document, options) =>
            {
                operation = "replace";
                candidate = document;
                cursorCondition = options.IfMatchEtag;
            })
            .Returns(batch.Object);
        batch.Setup(b => b.UpsertItem(It.IsAny<CursorDocument>(), null))
            .Callback<CursorDocument, TransactionalBatchItemRequestOptions>((document, _) =>
            {
                // Model the old unconditional operation so the regression can detect it.
                operation = "upsert";
                candidate = document;
            })
            .Returns(batch.Object);
        batch.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                bool hasMatchingCursor = (operation == "upsert") ||
                                         ((operation == "create") && actualCursor is null) ||
                                         ((operation == "replace") && (cursorCondition == actualETag));
                bool canCommit = hasPending && (pendingCondition == "append-attempt") && hasMatchingCursor;
                if (canCommit)
                {
                    actualCursor = candidate!.Position;
                    hasPending = false;
                }

                Mock<TransactionalBatchResponse> response = new();
                response.SetupGet(r => r.IsSuccessStatusCode).Returns(canCommit);
                response.SetupGet(r => r.StatusCode)
                    .Returns(canCommit ? HttpStatusCode.OK : HttpStatusCode.PreconditionFailed);
                return response.Object;
            });
        Mock<IMapper<CursorDocument, CursorStorageModel>> cursorMapper = new();
        cursorMapper.Setup(m => m.Map(It.IsAny<CursorDocument>()))
            .Returns((CursorDocument document) => new()
            {
                Position = new(document.Position),
            });
        Mock<IRetryPolicy> retry = new(MockBehavior.Strict);
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<CursorStorageModel?>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CursorStorageModel?>> action, CancellationToken _) => action());
        CosmosRepository repository = new(
            container.Object,
            retry.Object,
            cursorMapper.Object,
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        if (hasAdvancedCursor)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CommitCursorPositionAsync(
                key,
                3,
                "append-attempt",
                TestContext.Current.CancellationToken));
            Assert.Equal(10, actualCursor);
            Assert.True(hasPending);
        }
        else
        {
            await repository.CommitCursorPositionAsync(key, 3, "append-attempt", TestContext.Current.CancellationToken);
            Assert.Equal(3, actualCursor);
            Assert.False(hasPending);
        }

        Assert.Equal(isMissingSnapshot ? "create" : "replace", operation);
        Assert.Equal("append-attempt", pendingCondition);
        Assert.Equal(isMissingSnapshot ? null : "observed-cursor", cursorCondition);
        batch.Verify(b => b.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
        batch.Verify(b => b.UpsertItem(It.IsAny<CursorDocument>(), null), Times.Never);
        container.Verify(
            c => c.ReadItemAsync<CursorDocument>("cursor", new(key.ToString()), null, It.IsAny<CancellationToken>()),
            Times.Once);
        container.Verify(c => c.CreateTransactionalBatch(new(key.ToString())), Times.Once);
        container.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Rejects a cursor snapshot that cannot safely advance before constructing a mutation.
    /// </summary>
    /// <param name="observedPosition">The committed cursor position returned by storage.</param>
    /// <param name="observedETag">The cursor version returned by storage.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(3, "observed-cursor")]
    [InlineData(10, "observed-cursor")]
    [InlineData(1, "")]
    public async Task CursorCommitRejectsNonAdvancingOrUnversionedSnapshotAsync(
        long observedPosition,
        string observedETag
    )
    {
        BrookKey key = new("test", "invalid-cursor-snapshot");
        Mock<ItemResponse<CursorDocument>> response = new();
        response.SetupGet(r => r.Resource)
            .Returns(
                new CursorDocument
                {
                    Position = observedPosition,
                });
        response.SetupGet(r => r.ETag).Returns(observedETag);
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor",
                new(key.ToString()),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
        Mock<IMapper<CursorDocument, CursorStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<CursorDocument>()))
            .Returns((CursorDocument document) => new()
            {
                Position = new(document.Position),
            });
        Mock<IRetryPolicy> retry = new(MockBehavior.Strict);
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<CursorStorageModel?>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CursorStorageModel?>> action, CancellationToken _) => action());
        CosmosRepository repository = new(
            container.Object,
            retry.Object,
            mapper.Object,
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CommitCursorPositionAsync(
            key,
            3,
            "append-attempt",
            TestContext.Current.CancellationToken));
        container.Verify(
            c => c.ReadItemAsync<CursorDocument>("cursor", new(key.ToString()), null, It.IsAny<CancellationToken>()),
            Times.Once);
        container.Verify(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>()), Times.Never);
        container.VerifyNoOtherCalls();
    }
}