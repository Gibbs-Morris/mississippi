using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Tributary.Runtime.Storage.Blobs;

using Moq;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests;

/// <summary>
///     Tests container initialization at host startup.
/// </summary>
public sealed class SnapshotBlobContainerInitializerTests
{
    /// <summary>
    ///     Verifies initialization rejects missing dependencies.
    /// </summary>
    [Fact]
    public void ConstructorShouldRejectMissingDependencies()
    {
        ArgumentNullException operationsException = Assert.Throws<ArgumentNullException>(() =>
            new SnapshotBlobContainerInitializer(null!, NullLogger<SnapshotBlobContainerInitializer>.Instance));
        ArgumentNullException loggerException = Assert.Throws<ArgumentNullException>(() =>
            new SnapshotBlobContainerInitializer(Mock.Of<ISnapshotBlobOperations>(), null!));
        Assert.Equal("operations", operationsException.ParamName);
        Assert.Equal("logger", loggerException.ParamName);
    }

    /// <summary>
    ///     Verifies startup creates the container with the host cancellation token.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StartAsyncShouldCreateContainer()
    {
        using CancellationTokenSource cancellation = new();
        Mock<ISnapshotBlobOperations> operations = new(MockBehavior.Strict);
        operations.Setup(value => value.CreateContainerIfNotExistsAsync(cancellation.Token))
            .Returns(Task.CompletedTask);
        SnapshotBlobContainerInitializer initializer = new(
            operations.Object,
            NullLogger<SnapshotBlobContainerInitializer>.Instance);
        await initializer.StartAsync(cancellation.Token);
        operations.Verify(value => value.CreateContainerIfNotExistsAsync(cancellation.Token), Times.Once);
        operations.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Verifies startup cancellation is propagated to the host.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StartAsyncShouldPropagateCancellation()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Mock<ISnapshotBlobOperations> operations = new(MockBehavior.Strict);
        operations.Setup(value => value.CreateContainerIfNotExistsAsync(cancellation.Token))
            .Returns(Task.FromCanceled(cancellation.Token));
        SnapshotBlobContainerInitializer initializer = new(
            operations.Object,
            NullLogger<SnapshotBlobContainerInitializer>.Instance);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initializer.StartAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    /// <summary>
    ///     Verifies storage initialization failures prevent successful startup.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StartAsyncShouldPropagateStorageFailure()
    {
        InvalidOperationException failure = new("Container creation failed.");
        Mock<ISnapshotBlobOperations> operations = new(MockBehavior.Strict);
        operations.Setup(value => value.CreateContainerIfNotExistsAsync(CancellationToken.None)).ThrowsAsync(failure);
        SnapshotBlobContainerInitializer initializer = new(
            operations.Object,
            NullLogger<SnapshotBlobContainerInitializer>.Instance);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            initializer.StartAsync(CancellationToken.None));
        Assert.Same(failure, exception);
    }

    /// <summary>
    ///     Verifies shutdown leaves persisted snapshots intact.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StopAsyncShouldNotAccessStorage()
    {
        Mock<ISnapshotBlobOperations> operations = new(MockBehavior.Strict);
        SnapshotBlobContainerInitializer initializer = new(
            operations.Object,
            NullLogger<SnapshotBlobContainerInitializer>.Instance);
        await initializer.StopAsync(CancellationToken.None);
        operations.VerifyNoOtherCalls();
    }
}