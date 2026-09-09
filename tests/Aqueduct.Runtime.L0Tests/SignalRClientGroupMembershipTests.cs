using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies cleanup ownership and failure handling without an Orleans host.
/// </summary>
public sealed class SignalRClientGroupMembershipTests
{
    private static SignalRClientGrain CreateGrain(
        IGrainFactory factory
    ) =>
        new(
            GrainContextMockBuilder.Create().WithGrainKey("hub:connection").BuildObject(),
            factory,
            Options.Create(new AqueductOptions()),
            NullLogger<SignalRClientGrain>.Instance,
            new FakeTimeProvider());

    /// <summary>
    ///     Construction requires the factory used for group cleanup.
    /// </summary>
    [Fact]
    public void ConstructorShouldRejectMissingGrainFactory() =>
        Assert.Throws<ArgumentNullException>(() => CreateGrain(null!));

    /// <summary>
    ///     A client without a connection cannot create group membership.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task DisconnectedClientShouldIgnoreGroupOperations()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        SignalRClientGrain client = CreateGrain(factory);
        await client.AddToGroupAsync("group");
        await client.RemoveFromGroupAsync("group");
        Assert.Empty(factory.ReceivedCalls());
    }

    /// <summary>
    ///     Duplicate joins require only one removal during disconnect.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task DuplicateJoinsShouldBeCleanedOnce()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>("hub:group").Returns(group);
        SignalRClientGrain client = CreateGrain(factory);
        await client.ConnectAsync("hub", "server");
        await client.AddToGroupAsync("group");
        await client.AddToGroupAsync("group");
        await client.DisconnectAsync();
        await client.DisconnectAsync();
        await group.Received(1).RemoveConnectionAsync("connection");
        Assert.Null(await client.GetServerIdAsync());
    }

    /// <summary>
    ///     Explicit removal updates cleanup ownership, including a subsequent rejoin.
    /// </summary>
    /// <param name="rejoin">Whether the client rejoins before disconnecting.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitRemovalShouldUpdateCleanupOwnership(
        bool rejoin
    )
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>("hub:group").Returns(group);
        SignalRClientGrain client = CreateGrain(factory);
        await client.ConnectAsync("hub", "server");
        await client.AddToGroupAsync("group");
        await client.RemoveFromGroupAsync("group");
        if (rejoin)
        {
            await client.AddToGroupAsync("group");
        }

        await client.DisconnectAsync();
        await group.Received(rejoin ? 2 : 1).RemoveConnectionAsync("connection");
    }

    /// <summary>
    ///     Cleanup attempts every group and retains failed removals without allowing new joins.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task FailedDisconnectShouldRetainOnlyUnremovedGroupsForRetry()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain failed = Substitute.For<ISignalRGroupGrain>();
        ISignalRGroupGrain healthy = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>("hub:failed").Returns(failed);
        factory.GetGrain<ISignalRGroupGrain>("hub:healthy").Returns(healthy);
        SignalRClientGrain client = CreateGrain(factory);
        await client.ConnectAsync("hub", "server");
        await client.AddToGroupAsync("failed");
        await client.AddToGroupAsync("healthy");
        failed.RemoveConnectionAsync("connection")
            .Returns(Task.FromException(new InvalidOperationException("unavailable")), Task.CompletedTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DisconnectAsync());
        Assert.Null(await client.GetServerIdAsync());
        await healthy.Received(1).RemoveConnectionAsync("connection");
        await client.AddToGroupAsync("healthy");
        await healthy.Received(1).AddConnectionAsync("connection");
        await client.DisconnectAsync();
        await failed.Received(2).RemoveConnectionAsync("connection");
        await healthy.Received(1).RemoveConnectionAsync("connection");
    }

    /// <summary>
    ///     An uncertain add outcome retains cleanup ownership.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task FailedJoinShouldStillBeCleanedOnDisconnect()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>("hub:group").Returns(group);
        group.AddConnectionAsync("connection").Returns(Task.FromException(new InvalidOperationException("uncertain")));
        SignalRClientGrain client = CreateGrain(factory);
        await client.ConnectAsync("hub", "server");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.AddToGroupAsync("group"));
        await client.DisconnectAsync();
        await group.Received(1).RemoveConnectionAsync("connection");
    }

    /// <summary>
    ///     Failed explicit removal keeps the group eligible for disconnect cleanup.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task FailedRemovalShouldBeRetriedOnDisconnect()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRGroupGrain group = Substitute.For<ISignalRGroupGrain>();
        factory.GetGrain<ISignalRGroupGrain>("hub:group").Returns(group);
        SignalRClientGrain client = CreateGrain(factory);
        await client.ConnectAsync("hub", "server");
        await client.AddToGroupAsync("group");
        group.RemoveConnectionAsync("connection")
            .Returns(Task.FromException(new InvalidOperationException("unavailable")), Task.CompletedTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.RemoveFromGroupAsync("group"));
        await client.DisconnectAsync();
        await group.Received(2).RemoveConnectionAsync("connection");
    }

    /// <summary>
    ///     Group names are validated before any remote mutation.
    /// </summary>
    /// <param name="groupName">The invalid group name.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GroupOperationsShouldRejectInvalidNames(
        string? groupName
    )
    {
        SignalRClientGrain client = CreateGrain(Substitute.For<IGrainFactory>());
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.AddToGroupAsync(groupName!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.RemoveFromGroupAsync(groupName!));
    }
}