using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Gateway.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Verifies group cleanup through gateway managers and real Orleans grains.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class AqueductHubGroupMembershipTests
{
    private static AqueductGrainFactory CreateGrainFactory() =>
        new(TestClusterAccess.Cluster.Client, NullLogger<AqueductGrainFactory>.Instance);

    private static AqueductHubLifetimeManager<TestAqueductHub> CreateManager(
        string serverId
    )
    {
        IServerIdProvider serverIdProvider = Substitute.For<IServerIdProvider>();
        serverIdProvider.ServerId.Returns(serverId);
        IStreamSubscriptionManager subscriptions = Substitute.For<IStreamSubscriptionManager>();
        subscriptions.IsInitialized.Returns(true);
        return new(
            serverIdProvider,
            CreateGrainFactory(),
            new ConnectionRegistry(),
            Substitute.For<ILocalMessageSender>(),
            Substitute.For<IHeartbeatManager>(),
            subscriptions,
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
    }

    /// <summary>
    ///     Concurrent broadcasts, group changes, and disconnect complete without leaving stale membership.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentGroupOperationsAndDisconnectShouldPreserveMembership()
    {
        const string ConnectionId = nameof(ConcurrentGroupOperationsAndDisconnectShouldPreserveMembership);
        using AqueductHubLifetimeManager<TestAqueductHub> owner = CreateManager("owner");
        using AqueductHubLifetimeManager<TestAqueductHub> remote = CreateManager("remote");
        HubConnectionContext connection = HubConnectionContextFactory.Create(ConnectionId);
        HubConnectionContext other = HubConnectionContextFactory.Create(ConnectionId + "-other");
        ISignalRGroupGrain group = CreateGrainFactory().GetGroupGrain(nameof(TestAqueductHub), ConnectionId);
        await owner.OnConnectedAsync(connection);
        await remote.OnConnectedAsync(other);
        await owner.AddToGroupAsync(ConnectionId, ConnectionId, TestContext.Current.CancellationToken);
        await remote.AddToGroupAsync(other.ConnectionId, ConnectionId, TestContext.Current.CancellationToken);
        List<Task> operations = [];
        for (int i = 0; i < 20; i++)
        {
            operations.Add(remote.SendGroupAsync(ConnectionId, "update", [], TestContext.Current.CancellationToken));
            operations.Add(
                remote.RemoveFromGroupAsync(ConnectionId, ConnectionId, TestContext.Current.CancellationToken));
            operations.Add(remote.AddToGroupAsync(ConnectionId, ConnectionId, TestContext.Current.CancellationToken));
        }

        operations.Add(owner.OnDisconnectedAsync(connection));
        operations.Add(remote.AddToGroupAsync(ConnectionId, ConnectionId, TestContext.Current.CancellationToken));
        await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        Assert.Equal(other.ConnectionId, Assert.Single(await group.GetConnectionsAsync()));
        await remote.OnDisconnectedAsync(other);
        Assert.Empty(await group.GetConnectionsAsync());
    }

    /// <summary>
    ///     Disconnect removes groups joined through both the owning and another gateway.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task OnDisconnectedAsyncShouldRemoveConnectionFromJoinedGroups()
    {
        const string ConnectionId = nameof(OnDisconnectedAsyncShouldRemoveConnectionFromJoinedGroups);
        using AqueductHubLifetimeManager<TestAqueductHub> owner = CreateManager("owner");
        using AqueductHubLifetimeManager<TestAqueductHub> remote = CreateManager("remote");
        HubConnectionContext connection = HubConnectionContextFactory.Create(ConnectionId);
        AqueductGrainFactory grains = CreateGrainFactory();
        ISignalRGroupGrain first = grains.GetGroupGrain(nameof(TestAqueductHub), ConnectionId + "-first");
        ISignalRGroupGrain second = grains.GetGroupGrain(nameof(TestAqueductHub), ConnectionId + "-second");
        await owner.OnConnectedAsync(connection);
        await owner.AddToGroupAsync(ConnectionId, ConnectionId + "-first", TestContext.Current.CancellationToken);
        await remote.AddToGroupAsync(ConnectionId, ConnectionId + "-second", TestContext.Current.CancellationToken);
        Assert.Contains(ConnectionId, await first.GetConnectionsAsync());
        Assert.Contains(ConnectionId, await second.GetConnectionsAsync());
        await owner.OnDisconnectedAsync(connection);
        Assert.Empty(await first.GetConnectionsAsync());
        Assert.Empty(await second.GetConnectionsAsync());
        Assert.Null(await grains.GetClientGrain(nameof(TestAqueductHub), ConnectionId).GetServerIdAsync());
    }
}