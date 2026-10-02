using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Inlet.Runtime.Grains;

using Orleans.Runtime;


namespace Mississippi.Aqueduct.Gateway.L2Tests;

/// <summary>
///     Exercises live volatile state and cleanup across real ordinary activation collection.
/// </summary>
[Collection("Idle routing collection")]
public sealed class IdleCollectionTests : IAsyncLifetime
{
    private readonly IdleCollectionFixture fixture = new();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => fixture.DisposeAsync();

    /// <inheritdoc />
    public ValueTask InitializeAsync() => fixture.InitializeAsync();

    private static string NewKey() => "IdleHub:" + Guid.NewGuid().ToString("N");

    private async Task ObserveProbeCollectionAsync(
        IAddressable probe
    )
    {
        GrainId id = probe.GetGrainId();
        Assert.Contains(await fixture.GetStatisticsAsync(), statistic => statistic.GrainId == id);
        await fixture.WaitUntilCollectedAsync(id);
    }

    /// <summary>
    ///     A connected route survives the collector pass which removes an unused client grain.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ConnectedRouteShouldSurviveIdleCollection()
    {
        ISignalRClientGrain client = fixture.Client.GetGrain<ISignalRClientGrain>(NewKey());
        await client.ConnectAsync("IdleHub", "idle-server");
        Assert.Equal("idle-server", await client.GetServerIdAsync());
        ISignalRClientGrain probe = fixture.Client.GetGrain<ISignalRClientGrain>(NewKey());
        Assert.Null(await probe.GetServerIdAsync());
        await ObserveProbeCollectionAsync(probe);
        Assert.Equal("idle-server", await client.GetServerIdAsync());
        await client.DisconnectAsync();
    }

    /// <summary>
    ///     Explicit disconnection releases a previously connected activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task DisconnectShouldReleaseConnectedActivation()
    {
        ISignalRClientGrain client = fixture.Client.GetGrain<ISignalRClientGrain>(NewKey());
        await client.ConnectAsync("IdleHub", "idle-server");
        await client.DisconnectAsync();
        await fixture.WaitUntilCollectedAsync(client.GetGrainId());
        Assert.Null(await client.GetServerIdAsync());
    }

    /// <summary>
    ///     Group membership survives the pass which removes an unused group activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task GroupMembershipShouldSurviveIdleCollection()
    {
        string connectionId = Guid.NewGuid().ToString("N");
        ISignalRClientGrain client = fixture.Client.GetGrain<ISignalRClientGrain>("IdleHub:" + connectionId);
        await client.ConnectAsync("IdleHub", "idle-server");
        string groupName = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain group = fixture.Client.GetGrain<ISignalRGroupGrain>("IdleHub:" + groupName);
        await client.AddToGroupAsync(groupName);
        Assert.Contains(connectionId, await group.GetConnectionsAsync());
        ISignalRGroupGrain probe = fixture.Client.GetGrain<ISignalRGroupGrain>(NewKey());
        Assert.Empty(await probe.GetConnectionsAsync());
        await ObserveProbeCollectionAsync(probe);
        Assert.Contains(connectionId, await group.GetConnectionsAsync());
        await client.DisconnectAsync();
    }

    /// <summary>
    ///     Removing the final member releases the group activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task LastGroupRemovalShouldReleaseActivation()
    {
        ISignalRGroupGrain group = fixture.Client.GetGrain<ISignalRGroupGrain>(NewKey());
        await group.AddConnectionAsync("idle-connection");
        await group.RemoveConnectionAsync("idle-connection");
        await fixture.WaitUntilCollectedAsync(group.GetGrainId());
        Assert.Empty(await group.GetConnectionsAsync());
    }

    /// <summary>
    ///     Removing the last projection also permits ordinary collection.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task LastUnsubscribeShouldReleaseActivation()
    {
        IInletSubscriptionGrain subscription = fixture.Client.GetGrain<IInletSubscriptionGrain>(NewKey());
        string id = await subscription.SubscribeAsync("idle-projection", Guid.NewGuid().ToString("N"));
        await subscription.UnsubscribeAsync(id);
        await fixture.WaitUntilCollectedAsync(subscription.GetGrainId());
        Assert.Empty(await subscription.GetSubscriptionsAsync());
    }

    /// <summary>
    ///     Projection interests survive the pass which removes an unused subscription activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ProjectionInterestShouldSurviveIdleCollection()
    {
        IInletSubscriptionGrain subscription = fixture.Client.GetGrain<IInletSubscriptionGrain>(NewKey());
        string id = await subscription.SubscribeAsync("idle-projection", Guid.NewGuid().ToString("N"));
        Assert.Equal(id, Assert.Single(await subscription.GetSubscriptionsAsync()).SubscriptionId);
        IInletSubscriptionGrain probe = fixture.Client.GetGrain<IInletSubscriptionGrain>(NewKey());
        Assert.Empty(await probe.GetSubscriptionsAsync());
        await ObserveProbeCollectionAsync(probe);
        Assert.Equal(id, Assert.Single(await subscription.GetSubscriptionsAsync()).SubscriptionId);
        await subscription.ClearAllAsync();
    }

    /// <summary>
    ///     Clearing subscriptions releases the subscribed activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task SubscriptionClearShouldReleaseActivation()
    {
        IInletSubscriptionGrain subscription = fixture.Client.GetGrain<IInletSubscriptionGrain>(NewKey());
        await subscription.SubscribeAsync("idle-projection", Guid.NewGuid().ToString("N"));
        await subscription.ClearAllAsync();
        await fixture.WaitUntilCollectedAsync(subscription.GetGrainId());
        Assert.Empty(await subscription.GetSubscriptionsAsync());
    }
}