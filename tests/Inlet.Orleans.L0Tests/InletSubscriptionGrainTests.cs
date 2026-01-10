using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Orleans.Grains;
using Mississippi.Inlet.Orleans.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Inlet.Orleans.L0Tests;

/// <summary>
///     Tests for <see cref="IInletSubscriptionGrain" /> operations.
/// </summary>
[AllureParentSuite("Inlet")]
[AllureSuite("Orleans")]
[AllureSubSuite("InletSubscriptionGrain")]
[Collection(ClusterTestSuite.Name)]
public sealed class InletSubscriptionGrainTests
{
    /// <summary>
    ///     Tests that ClearAllAsync removes all subscriptions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "ClearAllAsync Removes All Subscriptions")]
    public async Task ClearAllAsyncShouldRemoveAllSubscriptions()
    {
        // Arrange - test projections are pre-registered in TestSiloConfigurations
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-clear-all");
        await grain.SubscribeAsync(TestProjections.TestProjection, "entity-1");
        await grain.SubscribeAsync(TestProjections.TestProjection, "entity-2");
        ImmutableList<InletSubscription> before = await grain.GetSubscriptionsAsync();

        // Act
        await grain.ClearAllAsync();
        ImmutableList<InletSubscription> after = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Equal(2, before.Count);
        Assert.Empty(after);
    }

    /// <summary>
    ///     Tests that GetSubscriptionsAsync returns an empty list for a new grain.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetSubscriptionsAsync Returns Empty For New Grain")]
    public async Task GetSubscriptionsAsyncShouldReturnEmptyForNewGrain()
    {
        // Arrange
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-empty");

        // Act
        ImmutableList<InletSubscription> subscriptions = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Empty(subscriptions);
    }

    /// <summary>
    ///     Tests that multiple subscriptions can exist for same projection type.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "Multiple Subscriptions For Same Projection Type")]
    public async Task MultipleSubscriptionsForSameProjectionType()
    {
        // Arrange - test projections are pre-registered in TestSiloConfigurations
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-multi");

        // Act
        string sub1 = await grain.SubscribeAsync(TestProjections.TestProjection4, "entity-1");
        string sub2 = await grain.SubscribeAsync(TestProjections.TestProjection4, "entity-2");
        ImmutableList<InletSubscription> subscriptions = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Equal(2, subscriptions.Count);
        Assert.NotEqual(sub1, sub2);
    }

    /// <summary>
    ///     Tests that SubscribeAsync adds subscription to list.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SubscribeAsync Adds Subscription To List")]
    public async Task SubscribeAsyncShouldAddSubscriptionToList()
    {
        // Arrange - test projections are pre-registered in TestSiloConfigurations
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-sub-list");

        // Act
        string subscriptionId = await grain.SubscribeAsync(TestProjections.TestProjection2, "entity-1");
        ImmutableList<InletSubscription> subscriptions = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Single(subscriptions);
        Assert.Equal(subscriptionId, subscriptions[0].SubscriptionId);
        Assert.Equal(TestProjections.TestProjection2, subscriptions[0].ProjectionType);
        Assert.Equal("entity-1", subscriptions[0].EntityId);
    }

    /// <summary>
    ///     Tests that SubscribeAsync returns a subscription ID.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SubscribeAsync Returns Subscription Id")]
    public async Task SubscribeAsyncShouldReturnSubscriptionId()
    {
        // Arrange - test projections are pre-registered in TestSiloConfigurations
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-sub-1");

        // Act
        string subscriptionId = await grain.SubscribeAsync(TestProjections.TestProjection, "entity-1");

        // Assert
        Assert.NotNull(subscriptionId);
        Assert.NotEmpty(subscriptionId);
    }

    /// <summary>
    ///     Tests that SubscribeAsync throws when projection type is not registered.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SubscribeAsync Throws For Unregistered Projection Type")]
    public async Task SubscribeAsyncShouldThrowForUnregisteredProjectionType()
    {
        // Arrange
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-unreg");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.SubscribeAsync(
            "UnregisteredProjection",
            "entity-1"));
    }

    /// <summary>
    ///     Tests that SubscribeAsync throws when projection type is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SubscribeAsync Throws When Projection Type Is Null")]
    public async Task SubscribeAsyncShouldThrowWhenProjectionTypeIsNull()
    {
        // Arrange
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-null-proj");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.SubscribeAsync(null!, "entity-1"));
    }

    /// <summary>
    ///     Tests that UnsubscribeAsync is idempotent for unknown subscription.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnsubscribeAsync Is Idempotent For Unknown Subscription")]
    public async Task UnsubscribeAsyncShouldBeIdempotentForUnknownSubscription()
    {
        // Arrange
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-unsub-unknown");

        // Act - should not throw for unknown subscription
        Exception? exception = await Record.ExceptionAsync(() => grain.UnsubscribeAsync("unknown-subscription-id"));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that UnsubscribeAsync removes the subscription.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnsubscribeAsync Removes Subscription")]
    public async Task UnsubscribeAsyncShouldRemoveSubscription()
    {
        // Arrange - test projections are pre-registered in TestSiloConfigurations
        IInletSubscriptionGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<IInletSubscriptionGrain>("conn-unsub");
        string subscriptionId = await grain.SubscribeAsync(TestProjections.TestProjection3, "entity-1");
        ImmutableList<InletSubscription> before = await grain.GetSubscriptionsAsync();

        // Act
        await grain.UnsubscribeAsync(subscriptionId);
        ImmutableList<InletSubscription> after = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Single(before);
        Assert.Empty(after);
    }
}