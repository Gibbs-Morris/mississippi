using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests the interest snapshot used by reconnect recovery.
/// </summary>
public sealed class InletSignalRReconnectSnapshotTests : IAsyncDisposable
{
    private readonly InletSignalRActionEffect effect;

    private readonly Mock<HubConnection> hub = new(
        Mock.Of<IConnectionFactory>(),
        new JsonHubProtocol(),
        new IPEndPoint(IPAddress.Loopback, 80),
        Mock.Of<IServiceProvider>(),
        NullLoggerFactory.Instance);

    private Func<string?, Task> reconnected = _ => Task.CompletedTask;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRReconnectSnapshotTests" /> class.
    /// </summary>
    public InletSignalRReconnectSnapshotTests()
    {
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hub.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        provider.Setup(value => value.RegisterHandler(It.IsAny<string>(), It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        provider.Setup(value => value.OnReconnected(It.IsAny<Func<string?, Task>>()))
            .Callback<Func<string?, Task>>(handler => reconnected = handler);
        Mock<IProjectionFetcher> fetcher = new();
        fetcher.Setup(value => value.FetchAsync(
                typeof(TestProjection),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        Mock<IInletStore> store = new();
        ProjectionDtoRegistry registry = new();
        registry.Register("test/projections", typeof(TestProjection));
        effect = new(new(() => store.Object), provider.Object, fetcher.Object, registry);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await effect.DisposeAsync();
        await hub.Object.DisposeAsync();
    }

    private async Task<IAction[]> HandleAsync(
        IAction action
    )
    {
        List<IAction> results = [];
        await foreach (IAction result in effect.HandleAsync(action, new(), CancellationToken.None))
        {
            results.Add(result);
        }

        return results.ToArray();
    }

    /// <summary>
    ///     Interests established during reconnect recovery are not subscribed again by that recovery.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReconnectRestoresOnlyInterestsPresentAtItsStart()
    {
        int originalCalls = 0;
        IAction[]? newInterest = null;
        hub.Setup(value => value.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, Type _, object?[] arguments, CancellationToken _) =>
            {
                string entityId = (string)arguments[1]!;
                if ((entityId == "original") && (++originalCalls == 2))
                {
                    newInterest = await HandleAsync(new SubscribeToProjectionAction<TestProjection>("new"));
                }

                return entityId + "-id";
            });
        await HandleAsync(new SubscribeToProjectionAction<TestProjection>("original"));
        await reconnected("new-connection");
        Assert.NotNull(newInterest);
        Assert.Collection(
            newInterest,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
        hub.Verify(
            value => value.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.Is<object?[]>(arguments => (string?)arguments[1] == "new"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(2, originalCalls);
    }
}