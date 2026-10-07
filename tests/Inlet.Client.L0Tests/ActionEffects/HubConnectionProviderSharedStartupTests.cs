using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;

using Moq;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies shared startup outcomes without network I/O.
/// </summary>
public sealed class HubConnectionProviderSharedStartupTests
{
    /// <summary>
    ///     Canceling a joined caller leaves startup running for its owner.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancelledJoinedWaitLeavesOwnerRunning()
    {
        TaskCompletionSource startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int starts = 0;
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            null,
            null,
            async token =>
            {
                starts++;
                await startup.Task.WaitAsync(token);
            });
        using CancellationTokenSource joinedCancellation = new();
        Task owner = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        Task joined = provider.EnsureConnectedAsync(joinedCancellation.Token);
        await joinedCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            joined.WaitAsync(TestContext.Current.CancellationToken));
        Assert.False(owner.IsCompleted);
        Assert.Equal(1, starts);
        InvalidOperationException failure = new("startup failed after waiter cancellation");
        startup.SetException(failure);
        InvalidOperationException ownerFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            owner.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, ownerFailure);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRDisconnectedAction>()), Times.Once);
    }

    /// <summary>
    ///     Disposed providers reject readiness before starting a transport.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task DisposedProviderRejectsStartup()
    {
        int starts = 0;
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            null,
            null,
            _ =>
            {
                starts++;
                return Task.CompletedTask;
            });
        Func<Task> readiness = async () => await provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        Task disposing = provider.DisposeAsync().AsTask();
        await disposing.WaitAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ObjectDisposedException>(readiness);
        Assert.Equal(0, starts);
    }

    /// <summary>
    ///     Joined callers observe the same failure after one terminal status publication.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task JoinedCallersObserveSharedStartupFailure()
    {
        TaskCompletionSource startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int starts = 0;
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            null,
            null,
            async token =>
            {
                starts++;
                await startup.Task.WaitAsync(token);
            });
        Task owner = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        Task joined = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        Assert.False(joined.IsCompleted);
        Assert.Equal(1, starts);
        InvalidOperationException failure = new("shared startup failure");
        startup.SetException(failure);
        InvalidOperationException ownerFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            owner.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, ownerFailure);
        InvalidOperationException joinedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            joined.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, joinedFailure);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()), Times.Once);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRDisconnectedAction>()), Times.Once);
    }
}