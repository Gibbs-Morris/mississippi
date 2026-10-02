using System.Threading.Tasks;

using Mississippi.Inlet.Runtime.Abstractions;

using NSubstitute;

using Orleans.Runtime;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Verifies healthy, disconnected and temporarily unavailable routes through the owned cleanup callback.
/// </summary>
public sealed class InletSubscriptionLivenessTests
{
    /// <summary>
    ///     An unavailable route preserves all owned state so a later definitive response can release it.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task FailedLookupShouldPreserveSubscriptionAndAllowRetry()
    {
        using InletSubscriptionLivenessFixture fixture = new();
        string subscriptionId = await fixture.Grain.SubscribeAsync("projection", "entity");
        fixture.Client.GetServerIdAsync()
            .Returns(Task.FromException<string?>(new OrleansException("client unavailable")));
        await Assert.ThrowsAsync<OrleansException>(fixture.CleanupAsync);
        Assert.Equal(subscriptionId, Assert.Single(await fixture.Grain.GetSubscriptionsAsync()).SubscriptionId);
        await fixture.Handle.DidNotReceive().UnsubscribeAsync();
        fixture.Timer.DidNotReceive().Dispose();
        fixture.Client.GetServerIdAsync().Returns(Task.FromResult<string?>(null));
        await fixture.CleanupAsync();
        Assert.Empty(await fixture.Grain.GetSubscriptionsAsync());
        await fixture.Handle.Received(1).UnsubscribeAsync();
        fixture.Timer.Received(1).Dispose();
    }

    /// <summary>
    ///     A live route retains its subscription, stream observer and owned timer.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task HealthyRouteShouldRetainSubscriptionAndStream()
    {
        using InletSubscriptionLivenessFixture fixture = new();
        string subscriptionId = await fixture.Grain.SubscribeAsync("projection", "entity");
        await fixture.CleanupAsync();
        InletSubscription subscription = Assert.Single(await fixture.Grain.GetSubscriptionsAsync());
        Assert.Equal(subscriptionId, subscription.SubscriptionId);
        await fixture.Handle.DidNotReceive().UnsubscribeAsync();
        fixture.Timer.DidNotReceive().Dispose();
    }

    /// <summary>
    ///     A disconnected route releases the observer, subscriptions and cleanup timer.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MissingRouteShouldReleaseSubscriptionAndStream()
    {
        using InletSubscriptionLivenessFixture fixture = new();
        await fixture.Grain.SubscribeAsync("projection", "entity");
        fixture.Client.GetServerIdAsync().Returns(Task.FromResult<string?>(null));
        await fixture.CleanupAsync();
        Assert.Empty(await fixture.Grain.GetSubscriptionsAsync());
        await fixture.Handle.Received(1).UnsubscribeAsync();
        fixture.Timer.Received(1).Dispose();
    }
}