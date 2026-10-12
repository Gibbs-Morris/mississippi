using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Captures owned logging while preserving the actual group routing values.
/// </summary>
public sealed class AqueductUserGroupLoggingTests
{
    private const string UserGroupName =
        "__aqueduct_user__0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    private static void AssertGroupLogs(
        ILogger logger,
        string groupName,
        bool isReserved,
        int expectedCount
    )
    {
        object?[][] calls = logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .ToArray();
        Assert.Equal(expectedCount, calls.Length);
        foreach (object?[] call in calls)
        {
            Assert.Equal(LogLevel.Debug, call[0]);
            KeyValuePair<string, object?>[] state =
                Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(call[2], false).ToArray();
            Delegate formatter = Assert.IsType<Delegate>(call[4], false);
            string message = Assert.IsType<string>(formatter.DynamicInvoke(call[2], call[3]));
            string loggedGroupName = Assert.IsType<string>(
                Assert.Single(state, field => field.Key == "GroupName").Value);
            if (isReserved)
            {
                Assert.DoesNotContain(groupName, message, StringComparison.Ordinal);
                Assert.DoesNotContain(groupName["__aqueduct_user__".Length..], message, StringComparison.Ordinal);
                Assert.All(
                    state,
                    field => Assert.DoesNotContain(
                        groupName["__aqueduct_user__".Length..],
                        field.Value?.ToString() ?? string.Empty,
                        StringComparison.Ordinal));
                Assert.Equal("[user-group]", loggedGroupName);
            }
            else
            {
                Assert.Equal(groupName, loggedGroupName);
                Assert.Contains(groupName, message, StringComparison.Ordinal);
            }
        }
    }

    private static AqueductHubLifetimeManager<TestAqueductHub> CreateManager(
        IAqueductGrainFactory grainFactory,
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger
    )
    {
        IServerIdProvider server = Substitute.For<IServerIdProvider>();
        server.ServerId.Returns("privacy-server");
        return new(
            server,
            grainFactory,
            Substitute.For<IConnectionRegistry>(),
            Substitute.For<ILocalMessageSender>(),
            Substitute.For<IHeartbeatManager>(),
            Substitute.For<IStreamSubscriptionManager>(),
            logger);
    }

    /// <summary>
    ///     Logging preserves the factory lookup for an existing default typed key.
    /// </summary>
    [Fact]
    public void DefaultGroupKeyLoggingPreservesFactoryLookup()
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain expected = Substitute.For<ISignalRGroupGrain>();
        orleans.GetGrain<ISignalRGroupGrain>(":").Returns(expected);
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        AqueductGrainFactory factory = new(orleans, logger);
        Assert.Same(expected, factory.GetGroupGrain(default));
        orleans.Received(1).GetGrain<ISignalRGroupGrain>(":");
        object?[] call = Assert.Single(
                logger.ReceivedCalls(),
                entry => entry.GetMethodInfo().Name == nameof(ILogger.Log))
            .GetArguments();
        IEnumerable<KeyValuePair<string, object?>> state =
            Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(call[2], false);
        Assert.Null(Assert.Single(state, field => field.Key == "GroupName").Value);
    }

    /// <summary>
    ///     Factory logs mask reserved names while resolving the exact original grain key.
    /// </summary>
    /// <param name="groupName">The original group name.</param>
    /// <param name="isReserved">Whether this is a private user group.</param>
    [Theory]
    [InlineData("ordinary-room", false)]
    [InlineData(UserGroupName, true)]
    public void FactoryLogsPreserveRoutingAndMaskUserGroup(
        string groupName,
        bool isReserved
    )
    {
        IGrainFactory orleans = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain expected = Substitute.For<ISignalRGroupGrain>();
        orleans.GetGrain<ISignalRGroupGrain>($"TestHub:{groupName}").Returns(expected);
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        AqueductGrainFactory factory = new(orleans, logger);
        Assert.Same(expected, factory.GetGroupGrain("TestHub", groupName));
        orleans.Received(1).GetGrain<ISignalRGroupGrain>($"TestHub:{groupName}");
        AssertGroupLogs(logger, groupName, isReserved, 1);
    }

    /// <summary>
    ///     Ordinary manager group operations retain their diagnostic group names.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ManagerOrdinaryGroupLogsRemainUnchanged()
    {
        const string groupName = "ordinary-room";
        IAqueductGrainFactory factory = Substitute.For<IAqueductGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetClientGrain(nameof(TestAqueductHub), "connection").Returns(client);
        factory.GetGroupGrain(nameof(TestAqueductHub), groupName).Returns(group);
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            Substitute.For<ILogger<AqueductHubLifetimeManager<TestAqueductHub>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(factory, logger);
        await manager.AddToGroupAsync("connection", groupName, TestContext.Current.CancellationToken);
        await manager.RemoveFromGroupAsync("connection", groupName, TestContext.Current.CancellationToken);
        await manager.SendGroupAsync("ordinary-room", "Update", [], TestContext.Current.CancellationToken);
        await client.Received(1).AddToGroupAsync(groupName);
        await client.Received(1).RemoveFromGroupAsync(groupName);
        await group.Received(1)
            .SendMessageAsync("Update", Arg.Is<ImmutableArray<object?>>(arguments => arguments.IsEmpty));
        AssertGroupLogs(logger, groupName, false, 3);
    }

    /// <summary>
    ///     A rejected reserved ordinary-group send must not expose its token before validation.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task RejectedReservedGroupSendDoesNotLogToken()
    {
        IAqueductGrainFactory factory = Substitute.For<IAqueductGrainFactory>();
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            Substitute.For<ILogger<AqueductHubLifetimeManager<TestAqueductHub>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(factory, logger);
        await Assert.ThrowsAsync<ArgumentException>(() => manager.SendGroupAsync(
            UserGroupName,
            "Update",
            [],
            TestContext.Current.CancellationToken));
        Assert.DoesNotContain(factory.ReceivedCalls(), call => call.GetMethodInfo().Name == "GetGroupGrain");
        AssertGroupLogs(logger, UserGroupName, true, 1);
    }

    /// <summary>
    ///     User sends retain their actual derived token while masking it in structured and formatted logs.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task UserSendDoesNotLogItsDerivedRoutingToken()
    {
        IAqueductGrainFactory factory = Substitute.For<IAqueductGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGroupGrain(nameof(TestAqueductHub), Arg.Any<string>()).Returns(group);
        ILogger<AqueductHubLifetimeManager<TestAqueductHub>> logger =
            Substitute.For<ILogger<AqueductHubLifetimeManager<TestAqueductHub>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = CreateManager(factory, logger);
        await manager.SendUserAsync("privacy@example.invalid", "Update", [], TestContext.Current.CancellationToken);
        string routedGroupName = Assert.IsType<string>(
            Assert.Single(factory.ReceivedCalls(), call => call.GetMethodInfo().Name == "GetGroupGrain")
                .GetArguments()[1]);
        Assert.StartsWith("__aqueduct_user__", routedGroupName, StringComparison.Ordinal);
        Assert.Equal("__aqueduct_user__".Length + 64, routedGroupName.Length);
        await group.Received(1)
            .SendMessageAsync("Update", Arg.Is<ImmutableArray<object?>>(arguments => arguments.IsEmpty));
        AssertGroupLogs(logger, routedGroupName, true, 1);
    }
}