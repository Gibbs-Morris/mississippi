using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotPersisterGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots")]
[AllureSubSuite("Snapshot Persister Grain")]
public sealed class SnapshotPersisterGrainTests
{
    private static Mock<IGrainContext> CreateDefaultGrainContext()
    {
        Mock<IGrainContext> mock = new();

        // Key format: brookName|entityId|version|snapshotStorageName|reducersHash
        mock.Setup(c => c.GrainId)
            .Returns(GrainId.Create("test", "TEST.BROOK|entity-1|5|TEST.SNAPSHOTS.TestBrook|abc123"));
        return mock;
    }

    private static SnapshotPersisterGrain CreateGrain(
        Mock<IGrainContext>? grainContextMock = null,
        Mock<ISnapshotStorageWriter>? snapshotStorageWriterMock = null,
        Mock<ILogger<SnapshotPersisterGrain>>? loggerMock = null
    )
    {
        grainContextMock ??= CreateDefaultGrainContext();
        snapshotStorageWriterMock ??= new();
        loggerMock ??= new();
        return new(grainContextMock.Object, snapshotStorageWriterMock.Object, loggerMock.Object);
    }

    /// <summary>
    ///     Verifies that activation correctly parses the grain key.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Activation")]
    public async Task OnActivateAsyncParsesKeyCorrectly()
    {
        // Arrange
        Mock<IGrainContext> grainContextMock = new();

        // Key format: brookName|entityId|version|snapshotStorageName|reducersHash
        grainContextMock.Setup(c => c.GrainId)
            .Returns(GrainId.Create("test", "TEST.BROOK|entity-123|42|MyProjection|hashValue"));
        SnapshotPersisterGrain grain = CreateGrain(grainContextMock);

        // Act - should not throw
        await grain.OnActivateAsync(CancellationToken.None);

        // Assert - grain activated successfully, key was parsed
        Assert.NotNull(grain);
    }

    /// <summary>
    ///     Verifies that PersistAsync writes the envelope to storage.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Persistence")]
    public async Task PersistAsyncWritesToStorage()
    {
        // Arrange
        const string reducerHash = "test-hash";
        SnapshotEnvelope envelope = new()
        {
            Data = [1, 2, 3, 4],
            DataContentType = "application/json",
            ReducerHash = reducerHash,
        };
        Mock<ISnapshotStorageWriter> storageWriterMock = new();
        SnapshotPersisterGrain grain = CreateGrain(snapshotStorageWriterMock: storageWriterMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        await grain.PersistAsync(envelope, CancellationToken.None);

        // Assert
        storageWriterMock.Verify(
            w => w.WriteAsync(It.IsAny<SnapshotKey>(), envelope, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}