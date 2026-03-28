using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Azure;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Deterministic transport-backed tests for <see cref="AzureBrookRepository" />.
/// </summary>
public sealed class AzureBrookRepositoryTests
{
    /// <summary>
    ///     Event blobs can be written, observed, read back, and deleted through the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task WriteReadAndDeleteEventAsyncRoundTripsCommittedEvent()
    {
        using AzureBlobTransportTestContext context = new();
        AzureBrookRepository repository = CreateRepository(context);
        BrookKey brookId = new("orders", "123");
        BrookEvent brookEvent = new()
        {
            Id = "event-1",
            Source = "orders",
            EventType = "OrderCreated",
            DataContentType = "application/json",
            Data = ImmutableArray.Create<byte>(1, 2, 3),
            DataSizeBytes = 3,
            Time = new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
        };

        await repository.WriteEventAsync(brookId, 2, brookEvent);

        Assert.True(await repository.EventExistsAsync(brookId, 2));

        List<BrookEvent> events = [];
        await foreach (BrookEvent item in repository.ReadEventsAsync(new BrookRangeKey("orders", "123", 2, 1)))
        {
            events.Add(item);
        }

        BrookEvent roundTripped = Assert.Single(events);
        Assert.Equal(brookEvent.Id, roundTripped.Id);
        Assert.Equal(brookEvent.EventType, roundTripped.EventType);
        Assert.Equal(brookEvent.Source, roundTripped.Source);
        Assert.Equal(brookEvent.Data.ToArray(), roundTripped.Data.ToArray());

        await repository.DeleteEventIfExistsAsync(brookId, 2);

        Assert.False(await repository.EventExistsAsync(brookId, 2));
    }

    /// <summary>
    ///     Missing cursor and pending documents resolve to <see langword="null" /> rather than leaking transport errors.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetCommittedCursorAndPendingWriteAsyncReturnNullWhenDocumentsMissing()
    {
        using AzureBlobTransportTestContext context = new();
        AzureBrookRepository repository = CreateRepository(context);
        BrookKey brookId = new("orders", "123");

        Assert.Null(await repository.GetCommittedCursorAsync(brookId));
        Assert.Null(await repository.GetPendingWriteAsync(brookId));
    }

    /// <summary>
    ///     Pending and cursor coordination documents honor Azure conditional-write semantics.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task PendingAndCursorDocumentsRespectConditionalBlobOperations()
    {
        using AzureBlobTransportTestContext context = new();
        AzureBrookRepository repository = CreateRepository(context);
        BrookKey brookId = new("orders", "123");
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt-1",
            CreatedUtc = new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
            EventCount = 2,
            LeaseId = "lease-1",
            OriginalPosition = 1,
            TargetPosition = 3,
            WriteEpoch = 3,
        };

        Assert.True(await repository.TryCreatePendingWriteAsync(brookId, pendingWrite));
        Assert.False(await repository.TryCreatePendingWriteAsync(brookId, pendingWrite));

        AzureBrookPendingWriteState persistedPending = await repository.GetPendingWriteAsync(brookId)
            ?? throw new InvalidOperationException("Pending write was not persisted.");
        Assert.Equal(pendingWrite.AttemptId, persistedPending.AttemptId);
        Assert.NotEqual(default, persistedPending.ETag);

        Assert.True(await repository.TryAdvanceCommittedCursorAsync(brookId, null, persistedPending));
        Assert.False(await repository.TryAdvanceCommittedCursorAsync(brookId, null, persistedPending));

        AzureBrookCommittedCursorState committedCursor = await repository.GetCommittedCursorAsync(brookId)
            ?? throw new InvalidOperationException("Committed cursor was not persisted.");
        Assert.Equal(3, committedCursor.Position);
        Assert.NotEqual(default, committedCursor.ETag);

        Assert.False(await repository.TryDeletePendingWriteAsync(
            brookId,
            persistedPending with
            {
                ETag = new ETag("\"etag-stale\""),
            }));
        Assert.True(await repository.TryDeletePendingWriteAsync(brookId, persistedPending));
        Assert.Null(await repository.GetPendingWriteAsync(brookId));
    }

    /// <summary>
    ///     Reads fail loudly when a committed range references a missing event blob.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadEventsAsyncThrowsWhenCommittedBlobMissing()
    {
        using AzureBlobTransportTestContext context = new();
        AzureBrookRepository repository = CreateRepository(context);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (BrookEvent brookEvent in repository.ReadEventsAsync(new BrookRangeKey("orders", "123", 5, 1)))
            {
                GC.KeepAlive(brookEvent);
            }
        });

        Assert.Contains("position 5", exception.Message, StringComparison.Ordinal);
        Assert.Contains("orders", exception.Message, StringComparison.Ordinal);
        Assert.Contains("123", exception.Message, StringComparison.Ordinal);
    }

    private static AzureBrookRepository CreateRepository(
        AzureBlobTransportTestContext context
    ) => new(
        context.BlobServiceClient,
        context.CreateOptions(),
        new Sha256StreamPathEncoder(),
        new AzureBrookEventDocumentCodec());
}