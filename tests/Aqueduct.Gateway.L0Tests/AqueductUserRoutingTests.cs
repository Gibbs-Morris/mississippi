using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>Verifies user routing through the actual default grain factory.</summary>
public sealed class AqueductUserRoutingTests
{
    /// <summary>Creates a manager using the real default factory and substituted grain endpoints.</summary>
    /// <param name="orleans">The Orleans factory endpoint.</param>
    /// <param name="client">The connection grain endpoint.</param>
    /// <param name="group">The group grain endpoint.</param>
    /// <param name="logger">The optional capturing manager logger.</param>
    /// <returns>The caller-owned manager.</returns>
    private static AqueductHubLifetimeManager<TestAqueductHub> CreateManager(
        IGrainFactory orleans,
        ISignalRClientGrain client,
        ISignalRGroupGrain group,
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>>? logger = null
    )
    {
        orleans.GetGrain<ISignalRClientGrain>(Arg.Any<string>()).Returns(client);
        orleans.GetGrain<ISignalRGroupGrain>(Arg.Any<string>()).Returns(group);
        group.GetConnectionsAsync().Returns(ImmutableHashSet<string>.Empty);
        IServerIdProvider server = Substitute.For<IServerIdProvider>();
        server.ServerId.Returns("user-routing-server");
        IStreamSubscriptionManager subscriptions = Substitute.For<IStreamSubscriptionManager>();
        subscriptions.IsInitialized.Returns(true);
        return new(
            server,
            new AqueductGrainFactory(orleans, NullLogger<AqueductGrainFactory>.Instance),
            new ConnectionRegistry(),
            Substitute.For<ILocalMessageSender>(),
            Substitute.For<IHeartbeatManager>(),
            subscriptions,
            logger ?? NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
    }

    /// <summary>Anonymous identities must not acquire a user group.</summary>
    /// <param name="userId">The absent or empty identity.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AnonymousConnectionsShouldNotJoinUserGroup(
        string? userId
    )
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            orleans,
            client,
            Substitute.For<ISignalRGroupGrain>());
        HubConnectionContext connection = HubConnectionContextFactory.Create("anonymous");
        connection.UserIdentifier = userId;
        await manager.OnConnectedAsync(connection);
        await client.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!);
    }

    /// <summary>Connection registration must record user membership before any send.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ConnectionShouldOwnUserMembershipBeforeSending()
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            orleans,
            client,
            Substitute.For<ISignalRGroupGrain>());
        HubConnectionContext connection = HubConnectionContextFactory.Create("registered");
        connection.UserIdentifier = "alice";
        await manager.OnConnectedAsync(connection);
        await client.Received(1).ConnectAsync(nameof(TestAqueductHub), "user-routing-server");
        await client.Received(1).AddToGroupAsync(Arg.Is<string>(name => (name.Length > 0) && !name.Contains(':')));
    }

    /// <summary>User sends must dispatch to the same valid group owned by their connection.</summary>
    /// <param name="userId">The case-sensitive user identifier.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData("alice")]
    [InlineData("alice:bob")]
    [InlineData("alice%3Abob")]
    [InlineData("Alice")]
    [InlineData("alice@example.com")]
    [InlineData("é🌊")]
    public async Task DefaultFactoryShouldDispatchToOwnedUserGroup(
        string userId
    )
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(orleans, client, group);
        HubConnectionContext connection = HubConnectionContextFactory.Create("owned");
        connection.UserIdentifier = userId;
        await manager.OnConnectedAsync(connection);
        await manager.SendUserAsync(userId, "update", ["payload"], TestContext.Current.CancellationToken);
        string rawKey = Assert.IsType<string>(
            Assert.Single(
                    orleans.ReceivedCalls(),
                    call => (call.GetMethodInfo().Name == "GetGrain") &&
                            call.GetMethodInfo().GetGenericArguments().Contains(typeof(ISignalRGroupGrain)))
                .GetArguments()[0]);
        SignalRGroupKey key = SignalRGroupKey.Parse(rawKey);
        Assert.Equal(nameof(TestAqueductHub), key.HubName);
        await client.Received(1).AddToGroupAsync(key.GroupName);
        await group.Received(1)
            .SendMessageAsync(
                "update",
                Arg.Is<ImmutableArray<object?>>(args => (args.Length == 1) && Equals(args[0], "payload")));
    }

    /// <summary>Ordinary group operations must not alter the reserved user namespace.</summary>
    /// <param name="operation">The ordinary group operation.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData("join")]
    [InlineData("remove")]
    [InlineData("send")]
    [InlineData("except")]
    public async Task OrdinaryGroupOperationsShouldRejectReservedUserNamespace(
        string operation
    )
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            orleans,
            client,
            Substitute.For<ISignalRGroupGrain>());
        const string GroupName = "__aqueduct_user__alice";
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => operation switch
        {
            "join" => manager.AddToGroupAsync("conn", GroupName, cancellation),
            "remove" => manager.RemoveFromGroupAsync("conn", GroupName, cancellation),
            "send" => manager.SendGroupAsync(GroupName, "update", [], cancellation),
            "except" => manager.SendGroupExceptAsync(GroupName, "update", [], Array.Empty<string>(), cancellation),
            var _ => throw new InvalidOperationException("Unknown test group operation."),
        });
        Assert.Equal("groupName", exception.ParamName);
        Assert.DoesNotContain(orleans.ReceivedCalls(), call => call.GetMethodInfo().Name == "GetGrain");
    }

    /// <summary>Oversized user keys must fail before acquiring connection or group state.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task OversizedUserIdentifierShouldFailBeforeRegistration()
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            orleans,
            client,
            Substitute.For<ISignalRGroupGrain>());
        HubConnectionContext connection = HubConnectionContextFactory.Create("oversized");
        connection.UserIdentifier = new('a', 5000);
        await Assert.ThrowsAsync<ArgumentException>(() => manager.OnConnectedAsync(connection));
        Assert.DoesNotContain(orleans.ReceivedCalls(), call => call.GetMethodInfo().Name == "GetGrain");
        await client.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default!);
    }

    /// <summary>Group keys and their logs must not disclose a reversibly encoded user identity.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UserRoutingShouldNotExposeIdentityInKeysOrLogs()
    {
        const string UserId = "person@example.invalid";
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            Substitute.For<ILogger<AqueductHubLifetimeManager<TestAqueductHub>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(
            orleans,
            Substitute.For<ISignalRClientGrain>(),
            Substitute.For<ISignalRGroupGrain>(),
            logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("privacy-control");
        connection.UserIdentifier = UserId;
        await manager.OnConnectedAsync(connection);
        await manager.SendUserAsync(UserId, "privacy", [], TestContext.Current.CancellationToken);
        string rawKey = Assert.IsType<string>(
            Assert.Single(
                    orleans.ReceivedCalls(),
                    call => (call.GetMethodInfo().Name == "GetGrain") &&
                            call.GetMethodInfo().GetGenericArguments().Contains(typeof(ISignalRGroupGrain)))
                .GetArguments()[0]);
        string logs = string.Join(
            "\n",
            logger.ReceivedCalls()
                .Where(call => call.GetMethodInfo().Name == "Log")
                .Select(call => call.GetArguments()[2]?.ToString()));
        Assert.NotEmpty(logs);
        Assert.DoesNotContain(UserId, rawKey, StringComparison.Ordinal);
        Assert.DoesNotContain(Uri.EscapeDataString(UserId), rawKey, StringComparison.Ordinal);
        Assert.DoesNotContain(UserId, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(Uri.EscapeDataString(UserId), logs, StringComparison.Ordinal);
    }
}