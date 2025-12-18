using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots.Tests;

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
        mock.Setup(c => c.GrainId).Returns(GrainId.Create("test", "TEST.SNAPSHOTS.TestBrook|entity-1|abc123|5"));
        return mock;
    }

    private static TestableSnapshotPersisterGrain CreateGrain(
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
    ///     Testable snapshot persister grain that exposes internal type for testing.
    /// </summary>
    private sealed class TestableSnapshotPersisterGrain : SnapshotPersisterGrain
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestableSnapshotPersisterGrain" /> class.
        /// </summary>
        /// <param name="grainContext">The grain context.</param>
        /// <param name="snapshotStorageWriter">The snapshot storage writer.</param>
        /// <param name="logger">The logger.</param>
        public TestableSnapshotPersisterGrain(
            IGrainContext grainContext,
            ISnapshotStorageWriter snapshotStorageWriter,
            ILogger<SnapshotPersisterGrain> logger
        )
            : base(grainContext, snapshotStorageWriter, logger)
        {
        }
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
        grainContextMock.Setup(c => c.GrainId).Returns(GrainId.Create("test", "MyProjection|entity-123|hashValue|42"));
        TestableSnapshotPersisterGrain grain = CreateGrain(grainContextMock);

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
        TestableSnapshotPersisterGrain grain = CreateGrain(snapshotStorageWriterMock: storageWriterMock);
        await grain.OnActivateAsync(CancellationToken.None);

        // Act
        await grain.PersistAsync(envelope, CancellationToken.None);

        // Assert
        storageWriterMock.Verify(
            w => w.WriteAsync(It.IsAny<SnapshotKey>(), envelope, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}