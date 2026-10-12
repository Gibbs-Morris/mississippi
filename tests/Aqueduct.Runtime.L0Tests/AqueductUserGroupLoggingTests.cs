using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Grains;

using NSubstitute;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Captures owned grain and factory logs without starting an Orleans cluster.
/// </summary>
public sealed class AqueductUserGroupLoggingTests
{
    private const string HubName = "PrivacyHub";

    private const string UserGroupName =
        "__aqueduct_user__0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    private static void AssertGroupLogs(
        ILogger logger,
        string groupName,
        bool isReserved,
        int[] expectedEventIds,
        int expectedCount,
        bool hasInformationLogs
    )
    {
        object?[][] calls = logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .Where(call => Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(call[2], false)
                .Any(field => field.Key is "GroupName" or "GroupKey"))
            .ToArray();
        Assert.Equal(expectedCount, calls.Length);
        Assert.Equal(expectedEventIds.Order(), calls.Select(call => ((EventId)call[1]!).Id).Distinct().Order());
        Assert.Contains(calls, call => Equals(call[0], LogLevel.Debug));
        if (hasInformationLogs)
        {
            Assert.Contains(calls, call => Equals(call[0], LogLevel.Information));
        }

        foreach (object?[] call in calls.OrderByDescending(logCall => Equals(logCall[0], LogLevel.Information)))
        {
            KeyValuePair<string, object?>[] state =
                Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(call[2], false).ToArray();
            Delegate formatter = Assert.IsType<Delegate>(call[4], false);
            string message = Assert.IsType<string>(formatter.DynamicInvoke(call[2], call[3]));
            KeyValuePair<string, object?> groupField = Assert.Single(
                state,
                field => field.Key is "GroupName" or "GroupKey");
            string loggedValue = Assert.IsType<string>(groupField.Value);
            string expectedValue = groupField.Key == "GroupKey" ? $"{HubName}:{groupName}" : groupName;
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
                Assert.Equal(groupField.Key == "GroupKey" ? $"{HubName}:[user-group]" : "[user-group]", loggedValue);
            }
            else
            {
                Assert.Equal(expectedValue, loggedValue);
                Assert.Contains(expectedValue, message, StringComparison.Ordinal);
            }
        }
    }

    private static IGrainContext CreateContext(
        string key
    )
    {
        IGrainContext context = Substitute.For<IGrainContext>();
        context.GrainId.Returns(GrainId.Create("privacy-test", key));
        return context;
    }

    /// <summary>
    ///     Client join and removal retain the actual private membership key and redact only log values.
    /// </summary>
    /// <param name="groupName">The original group name.</param>
    /// <param name="isReserved">Whether this is a private user group.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData("ordinary-room", false)]
    [InlineData(UserGroupName, true)]
    public async Task ClientMembershipLogsPreserveRoutingAndMaskUserGroup(
        string groupName,
        bool isReserved
    )
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>($"{HubName}:{groupName}").Returns(group);
        ILogger<SignalRClientGrain> logger = Substitute.For<ILogger<SignalRClientGrain>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        SignalRClientGrain client = new(
            CreateContext($"{HubName}:connection"),
            factory,
            Options.Create(new AqueductOptions()),
            logger,
            new FakeTimeProvider());
        await client.ConnectAsync(HubName, "server");
        await client.AddToGroupAsync(groupName);
        await client.RemoveFromGroupAsync(groupName);
        await group.Received(1).AddConnectionAsync("connection");
        await group.Received(1).RemoveConnectionAsync("connection");
        factory.Received(2).GetGrain<ISignalRGroupGrain>($"{HubName}:{groupName}");
        AssertGroupLogs(logger, groupName, isReserved, [8, 9], 4, false);
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
        orleans.GetGrain<ISignalRGroupGrain>($"{HubName}:{groupName}").Returns(expected);
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        AqueductGrainFactory factory = new(orleans, logger);
        Assert.Same(expected, factory.GetGroupGrain(HubName, groupName));
        orleans.Received(1).GetGrain<ISignalRGroupGrain>($"{HubName}:{groupName}");
        AssertGroupLogs(logger, groupName, isReserved, [2], 1, false);
    }

    /// <summary>
    ///     Every group lifecycle log masks the private key while membership and fan-out keep it unchanged.
    /// </summary>
    /// <param name="groupName">The original group name.</param>
    /// <param name="isReserved">Whether this is a private user group.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData("ordinary-room", false)]
    [InlineData(UserGroupName, true)]
    public async Task GroupLifecycleLogsPreserveRoutingAndMaskUserGroup(
        string groupName,
        bool isReserved
    )
    {
        string originalKey = $"{HubName}:{groupName}";
        IGrainContext context = CreateContext(originalKey);
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        factory.GetGrain<ISignalRClientGrain>($"{HubName}:connection").Returns(client);
        ILogger<SignalRGroupGrain> logger = Substitute.For<ILogger<SignalRGroupGrain>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        SignalRGroupGrain group = new(context, factory, logger);
        await group.OnActivateAsync(TestContext.Current.CancellationToken);
        await group.AddConnectionAsync("connection");
        await group.AddConnectionAsync("connection");
        await group.RemoveConnectionAsync("missing");
        Assert.Equal(["connection"], await group.GetConnectionsAsync());
        await group.SendMessageAsync("Update", ImmutableArray<object?>.Empty);
        await group.RemoveConnectionAsync("connection");
        Assert.Empty(await group.GetConnectionsAsync());
        Assert.Equal(originalKey, group.GetPrimaryKeyString());
        await client.Received(1).SendMessageAsync("Update", ImmutableArray<object?>.Empty);
        factory.Received(1).GetGrain<ISignalRClientGrain>($"{HubName}:connection");
        AssertGroupLogs(logger, groupName, isReserved, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], 12, true);
    }
}