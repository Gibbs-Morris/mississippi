using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotCosmosRepository" />.
/// </summary>
/// <remarks>
///     These tests demonstrate the improved testability achieved by depending on
///     <see cref="ISnapshotContainerOperations" /> instead of the Cosmos SDK Container directly.
///     Following the Dependency Inversion Principle makes mocking straightforward.
/// </remarks>
public sealed class SnapshotCosmosRepositoryTests
{
    private static readonly SnapshotStreamKey StreamKey = new("TEST.BROOK", "type", "id", "hash");

    private static readonly SnapshotKey SnapshotKey = new(StreamKey, 3);

    private static SnapshotCosmosRepository CreateRepository(
        Mock<ISnapshotContainerOperations> containerOperations,
        IMapper<SnapshotDocument, SnapshotStorageModel>? documentToStorageMapper = null,
        IMapper<SnapshotStorageModel, SnapshotEnvelope>? storageToEnvelopeMapper = null,
        IMapper<SnapshotWriteModel, SnapshotStorageModel>? writeModelToStorageMapper = null,
        IMapper<SnapshotStorageModel, SnapshotDocument>? storageToDocumentMapper = null
    )
    {
        documentToStorageMapper ??= new StubMapper<SnapshotDocument, SnapshotStorageModel>(new());
        storageToEnvelopeMapper ??= new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new());
        writeModelToStorageMapper ??= new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new());
        storageToDocumentMapper ??= new StubMapper<SnapshotStorageModel, SnapshotDocument>(new());
        return new(
            containerOperations.Object,
            documentToStorageMapper,
            storageToEnvelopeMapper,
            writeModelToStorageMapper,
            storageToDocumentMapper,
            NullLogger<SnapshotCosmosRepository>.Instance);
    }

    private static async IAsyncEnumerable<SnapshotIdVersion> ToAsyncEnumerableAsync(
        IEnumerable<SnapshotIdVersion> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        foreach (SnapshotIdVersion item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    private sealed class StubMapper<TInput, TOutput> : IMapper<TInput, TOutput>
    {
        private readonly TOutput output;

        public StubMapper(
            TOutput output
        ) =>
            this.output = output;

        public TOutput Map(
            TInput input
        ) =>
            output;
    }

    /// <summary>
    ///     Verifies that constructor throws when containerOperations is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenContainerOperationsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            null!,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            NullLogger<SnapshotCosmosRepository>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when documentToStorageMapper is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenDocumentToStorageMapperIsNull()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            ops.Object,
            null!,
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            NullLogger<SnapshotCosmosRepository>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when storageToDocumentMapper is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenStorageToDocumentMapperIsNull()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            ops.Object,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            null!,
            NullLogger<SnapshotCosmosRepository>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when storageToEnvelopeMapper is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenStorageToEnvelopeMapperIsNull()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            ops.Object,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            null!,
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            NullLogger<SnapshotCosmosRepository>.Instance));
    }

    /// <summary>
    ///     Verifies that constructor throws when writeModelToStorageMapper is null.
    /// </summary>
    [Fact]
    public void ConstructorShouldThrowWhenWriteModelToStorageMapperIsNull()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        Assert.Throws<ArgumentNullException>(() => new SnapshotCosmosRepository(
            ops.Object,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(new()),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(new()),
            null!,
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(new()),
            NullLogger<SnapshotCosmosRepository>.Instance));
    }

    /// <summary>
    ///     Ensures DeleteAllAsync deletes all snapshots returned by the query.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldDeleteAllSnapshotsFromQuery()
    {
        List<string> deletedIds = [];
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.QuerySnapshotIdsAsync(StreamKey.ToString(), It.IsAny<CancellationToken>()))
            .Returns(
                ToAsyncEnumerableAsync(
                [
                    new("snap-1", 1),
                    new("snap-2", 2),
                    new("snap-3", 3),
                ]));
        ops.Setup(o => o.DeleteDocumentAsync(StreamKey.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((
                _,
                id,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(true);
        SnapshotCosmosRepository repository = CreateRepository(ops);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        Assert.Equal(["snap-1", "snap-2", "snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures DeleteAllAsync handles empty query results gracefully.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAllAsyncShouldHandleEmptyQueryResults()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.QuerySnapshotIdsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync([]));
        SnapshotCosmosRepository repository = CreateRepository(ops);
        await repository.DeleteAllAsync(StreamKey, CancellationToken.None);
        ops.Verify(
            o => o.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Ensures delete calls container operations with correct parameters.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task DeleteAsyncShouldCallContainerOperations()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.DeleteDocumentAsync(StreamKey.ToString(), "3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SnapshotCosmosRepository repository = CreateRepository(ops);
        await repository.DeleteAsync(SnapshotKey, CancellationToken.None);
        ops.Verify(o => o.DeleteDocumentAsync(StreamKey.ToString(), "3", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Ensures PruneAsync always retains the maximum version.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldAlwaysRetainMaxVersion()
    {
        List<string> deletedIds = [];
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.QuerySnapshotIdsAsync(StreamKey.ToString(), It.IsAny<CancellationToken>()))
            .Returns(
                ToAsyncEnumerableAsync(
                [
                    new("snap-3", 3),
                    new("snap-5", 5),
                ]));
        ops.Setup(o => o.DeleteDocumentAsync(StreamKey.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((
                _,
                id,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(true);
        SnapshotCosmosRepository repository = CreateRepository(ops);

        // No modulus matches, but max (5) should be retained
        await repository.PruneAsync(StreamKey, [10], CancellationToken.None);

        // Only snap-3 deleted; snap-5 is max so retained
        Assert.Equal(["snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures PruneAsync deletes snapshots that don't match the modulus rules.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldDeleteSnapshotsNotMatchingModulus()
    {
        List<string> deletedIds = [];
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.QuerySnapshotIdsAsync(StreamKey.ToString(), It.IsAny<CancellationToken>()))
            .Returns(
                ToAsyncEnumerableAsync(
                [
                    new("snap-1", 1),
                    new("snap-2", 2),
                    new("snap-3", 3),
                    new("snap-4", 4),
                    new("snap-5", 5),
                ]));
        ops.Setup(o => o.DeleteDocumentAsync(StreamKey.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((
                _,
                id,
                _
            ) => deletedIds.Add(id))
            .ReturnsAsync(true);
        SnapshotCosmosRepository repository = CreateRepository(ops);

        // Retain versions divisible by 2 (2, 4) and the max (5)
        await repository.PruneAsync(StreamKey, [2], CancellationToken.None);

        // snap-1 and snap-3 should be deleted (not divisible by 2, not max)
        Assert.Equal(["snap-1", "snap-3"], deletedIds);
    }

    /// <summary>
    ///     Ensures PruneAsync handles empty query results gracefully.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task PruneAsyncShouldHandleEmptyQueryResults()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.QuerySnapshotIdsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerableAsync([]));
        SnapshotCosmosRepository repository = CreateRepository(ops);
        await repository.PruneAsync(StreamKey, [1], CancellationToken.None);
        ops.Verify(
            o => o.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Ensures reads return mapped envelopes.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnEnvelope()
    {
        SnapshotDocument document = new()
        {
            Data = new byte[] { 1 },
            DataContentType = "ct",
        };
        SnapshotStorageModel storage = new()
        {
            Data = document.Data,
            DataContentType = document.DataContentType,
            StreamKey = StreamKey,
            Version = SnapshotKey.Version,
        };
        SnapshotEnvelope envelope = new()
        {
            Data = document.Data.ToImmutableArray(),
            DataContentType = document.DataContentType,
        };
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.ReadDocumentAsync(StreamKey.ToString(), "3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        SnapshotCosmosRepository repository = CreateRepository(
            ops,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(envelope),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(document));
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(envelope.Data, result!.Data);
        Assert.Equal(envelope.DataContentType, result.DataContentType);
    }

    /// <summary>
    ///     Ensures reads return null when document not found.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnNullWhenNotFound()
    {
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.ReadDocumentAsync(StreamKey.ToString(), "3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotDocument?)null);
        SnapshotCosmosRepository repository = CreateRepository(ops);
        SnapshotEnvelope? result = await repository.ReadAsync(SnapshotKey, CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    ///     Ensures writes map and upsert documents.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    public async Task WriteAsyncShouldUpsertDocument()
    {
        SnapshotEnvelope envelope = new()
        {
            Data = ImmutableArray.Create((byte)7),
            DataContentType = "bin",
        };
        SnapshotStorageModel storage = new()
        {
            StreamKey = StreamKey,
            Version = SnapshotKey.Version,
            Data = [.. envelope.Data],
            DataContentType = envelope.DataContentType,
        };
        SnapshotDocument document = new()
        {
            Id = "3",
            SnapshotPartitionKey = StreamKey.ToString(),
        };
        Mock<ISnapshotContainerOperations> ops = new();
        ops.Setup(o => o.UpsertDocumentAsync(StreamKey.ToString(), document, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SnapshotCosmosRepository repository = CreateRepository(
            ops,
            new StubMapper<SnapshotDocument, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotEnvelope>(envelope),
            new StubMapper<SnapshotWriteModel, SnapshotStorageModel>(storage),
            new StubMapper<SnapshotStorageModel, SnapshotDocument>(document));
        await repository.WriteAsync(SnapshotKey, envelope, CancellationToken.None);
        ops.Verify(
            o => o.UpsertDocumentAsync(StreamKey.ToString(), document, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}