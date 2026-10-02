using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Gateway.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>Verifies user recipients through real grains, the configured provider and gateway callbacks.</summary>
[Collection(ClusterTestSuite.Name)]
public sealed class AqueductNativeUserRoutingTests
{
    /// <summary>Colon, percent escapes and Unicode must identify separate user groups.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task EncodedIdentifiersShouldRemainDistinctAcrossGateways()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string colon = "id:" + suffix;
        string percent = "id%3A" + suffix;
        string unicode = "é🌊" + suffix;
        await using UserRoutingGateway owner = new(TestClusterAccess.Cluster.Client);
        await using UserRoutingGateway remote = new(TestClusterAccess.Cluster.Client);
        await owner.InitializeAsync();
        await remote.InitializeAsync();
        HubConnectionContext first = await owner.ConnectAsync(colon);
        _ = await owner.ConnectAsync(percent);
        HubConnectionContext second = await remote.ConnectAsync(unicode);
        _ = await remote.ConnectAsync(string.Empty);
        await owner.Manager.SendUserAsync(colon, "colon", [], TestContext.Current.CancellationToken);
        await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
        Assert.Equal(new[] { first.ConnectionId }, owner.GetRecipients("colon"));
        Assert.Empty(remote.GetRecipients("colon"));
        await remote.Manager.SendUsersAsync([colon, unicode], "encoded", [], TestContext.Current.CancellationToken);
        await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
        Assert.Equal(new[] { first.ConnectionId }, owner.GetRecipients("encoded"));
        Assert.Equal(new[] { second.ConnectionId }, remote.GetRecipients("encoded"));
    }

    /// <summary>Unpaired surrogate code units must not share another user's routing key.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MalformedIdentifiersShouldRemainDistinctAcrossGateways()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string firstId = "\ud800" + suffix;
        string secondId = "\ud801" + suffix;
        string replacementId = "\ufffd" + suffix;
        string literalEscapeId = "%uD800" + suffix;
        await using UserRoutingGateway owner = new(TestClusterAccess.Cluster.Client);
        await using UserRoutingGateway remote = new(TestClusterAccess.Cluster.Client);
        await owner.InitializeAsync();
        await remote.InitializeAsync();
        (string UserId, HubConnectionContext Connection, bool Local)[] targets =
        [
            (firstId, await owner.ConnectAsync(firstId), true),
            (secondId, await owner.ConnectAsync(secondId), true),
            (replacementId, await remote.ConnectAsync(replacementId), false),
            (literalEscapeId, await remote.ConnectAsync(literalEscapeId), false),
            ("\udc00" + suffix, await remote.ConnectAsync("\udc00" + suffix), false),
        ];
        foreach ((string userId, HubConnectionContext connection, bool local) in targets)
        {
            string method = "malformed-" + Guid.NewGuid().ToString("N");
            await owner.Manager.SendUserAsync(userId, method, [], TestContext.Current.CancellationToken);
            await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
            string[] expectedLocal = local ? [connection.ConnectionId] : [];
            string[] expectedRemote = local ? [] : [connection.ConnectionId];
            Assert.Equal(expectedLocal, owner.GetRecipients(method));
            Assert.Equal(expectedRemote, remote.GetRecipients(method));
        }
    }

    /// <summary>Single and multiple user sends isolate users across gateways and survive disconnect.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UserSendsShouldReachOnlyConnectedUsersAcrossGateways()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string alice = "Alice-" + suffix;
        string bob = "Bob-" + suffix;
        await using UserRoutingGateway owner = new(TestClusterAccess.Cluster.Client);
        await using UserRoutingGateway remote = new(TestClusterAccess.Cluster.Client);
        await owner.InitializeAsync();
        await remote.InitializeAsync();
        HubConnectionContext first = await owner.ConnectAsync(alice);
        HubConnectionContext second = await owner.ConnectAsync(alice);
        _ = await owner.ConnectAsync(alice.ToUpperInvariant());
        _ = await owner.ConnectAsync(null);
        HubConnectionContext third = await remote.ConnectAsync(alice);
        HubConnectionContext fourth = await remote.ConnectAsync(bob);
        ISignalRGroupGrain userGroup = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>(
            new SignalRGroupKey(nameof(TestAqueductHub), "__aqueduct_user__" + alice));
        Assert.Equal(
            new[] { first.ConnectionId, second.ConnectionId, third.ConnectionId }.Order(),
            (await userGroup.GetConnectionsAsync()).Order());
        await owner.Manager.SendUserAsync(alice, "single", [], TestContext.Current.CancellationToken);
        await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
        Assert.Equal(new[] { first.ConnectionId, second.ConnectionId }.Order(), owner.GetRecipients("single"));
        Assert.Equal(new[] { third.ConnectionId }, remote.GetRecipients("single"));
        await remote.Manager.SendUsersAsync([alice, bob], "multiple", [], TestContext.Current.CancellationToken);
        await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
        Assert.Equal(new[] { first.ConnectionId, second.ConnectionId }.Order(), owner.GetRecipients("multiple"));
        Assert.Equal(new[] { third.ConnectionId, fourth.ConnectionId }.Order(), remote.GetRecipients("multiple"));
        await owner.DisconnectAsync(first);
        Assert.Equal(
            new[] { second.ConnectionId, third.ConnectionId }.Order(),
            (await userGroup.GetConnectionsAsync()).Order());
        await remote.Manager.SendUserAsync(alice, "disconnected", [], TestContext.Current.CancellationToken);
        await Task.WhenAll(owner.FlushAsync(), remote.FlushAsync());
        Assert.Equal(new[] { second.ConnectionId }, owner.GetRecipients("disconnected"));
        Assert.Equal(new[] { third.ConnectionId }, remote.GetRecipients("disconnected"));
        Assert.Null(
            await TestClusterAccess.Cluster.Client
                .GetGrain<ISignalRClientGrain>(new SignalRClientKey(nameof(TestAqueductHub), first.ConnectionId))
                .GetServerIdAsync());
    }
}