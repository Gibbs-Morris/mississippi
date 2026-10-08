using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies that failed status publication preserves the startup outcome.
/// </summary>
public sealed class HubConnectionProviderFailurePublicationTests
{
    /// <summary>
    ///     A throwing Disconnected action retains the original cancellation and canceled task.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ThrowingDisconnectedDispatchRetainsOriginalCancellation()
    {
        Mock<IInletStore> store = new();
        InvalidOperationException dispatchFailure = new("disconnected publication failed");
        store.Setup(value => value.Dispatch(It.Is<IAction>(action => action is SignalRDisconnectedAction)))
            .Throws(dispatchFailure);
        Mock<ILogger<HubConnectionProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            logger: logger.Object);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        Task starting = provider.EnsureConnectedAsync(cancellation.Token);
        OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            starting.WaitAsync(TestContext.Current.CancellationToken));
        IInvocation primaryFailure = Assert.Single(
            logger.Invocations,
            invocation => (invocation.Method.Name == "Log") && invocation.Arguments[1] is EventId { Id: 3 });
        Assert.Same(primaryFailure.Arguments[3], observed);
        Assert.True(starting.IsCanceled);
        store.Verify(
            value => value.Dispatch(It.Is<IAction>(action => action is SignalRDisconnectedAction)),
            Times.Once);
    }
}