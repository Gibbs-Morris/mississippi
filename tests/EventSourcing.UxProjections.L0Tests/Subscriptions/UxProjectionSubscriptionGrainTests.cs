using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;
using Mississippi.EventSourcing.UxProjections.Subscriptions;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests.Subscriptions;

/// <summary>
///     Unit tests for <see cref="UxProjectionSubscriptionGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionSubscriptionGrain")]
public sealed class UxProjectionSubscriptionGrainTests
{
    private const string ConnectionId = "connection-abc123";

    private static UxProjectionSubscriptionGrain CreateGrain(
        string connectionId
    )
    {
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxProjectionSubscriptionGrain>> logger = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        GrainId grainId = GrainId.Create("ux-projection-subscription", connectionId);
        context.SetupGet(c => c.GrainId).Returns(grainId);
        return new(context.Object, options, streamIdFactory.Object, logger.Object);
    }

    /// <summary>
    ///     ClearAllAsync should remove all subscriptions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearAllAsyncRemovesAllSubscriptions()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);
        UxProjectionSubscriptionRequest request1 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "client-sub-1",
        };
        UxProjectionSubscriptionRequest request2 = new()
        {
            ProjectionType = "ChannelMessages",
            BrookType = "ChannelEvents",
            EntityId = "channel-456",
            ClientSubscriptionId = "client-sub-2",
        };
        await grain.SubscribeAsync(request1);
        await grain.SubscribeAsync(request2);

        // Act
        await grain.ClearAllAsync();

        // Assert
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();
        Assert.Empty(subscriptions);
    }

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<ILogger<UxProjectionSubscriptionGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionSubscriptionGrain(
            null!,
            options,
            streamIdFactory.Object,
            logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionSubscriptionGrain(
            context.Object,
            options,
            streamIdFactory.Object,
            null!));
    }

    /// <summary>
    ///     GetSubscriptionsAsync returns empty list when no subscriptions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSubscriptionsAsyncReturnsEmptyWhenNoSubscriptions()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);

        // Act
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();

        // Assert
        Assert.Empty(subscriptions);
    }

    /// <summary>
    ///     SubscribeAsync should return a subscription ID and add to state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SubscribeAsyncReturnsSubscriptionIdAndAddsToState()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);
        UxProjectionSubscriptionRequest request = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "client-sub-1",
        };

        // Act
        string subscriptionId = await grain.SubscribeAsync(request);

        // Assert
        Assert.NotNull(subscriptionId);
        Assert.NotEmpty(subscriptionId);
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();
        Assert.Single(subscriptions);
        Assert.Equal(request, subscriptions[0]);
    }

    /// <summary>
    ///     SubscribeAsync should return unique IDs for each subscription.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SubscribeAsyncReturnsUniqueIds()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);
        UxProjectionSubscriptionRequest request1 = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "client-sub-1",
        };
        UxProjectionSubscriptionRequest request2 = new()
        {
            ProjectionType = "ChannelMessages",
            BrookType = "ChannelEvents",
            EntityId = "channel-456",
            ClientSubscriptionId = "client-sub-2",
        };

        // Act
        string id1 = await grain.SubscribeAsync(request1);
        string id2 = await grain.SubscribeAsync(request2);

        // Assert
        Assert.NotEqual(id1, id2);
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();
        Assert.Equal(2, subscriptions.Count);
    }

    /// <summary>
    ///     UnsubscribeAsync should remove the subscription from state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnsubscribeAsyncRemovesSubscriptionFromState()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);
        UxProjectionSubscriptionRequest request = new()
        {
            ProjectionType = "UserProfile",
            BrookType = "UserEvents",
            EntityId = "user-123",
            ClientSubscriptionId = "client-sub-1",
        };
        string subscriptionId = await grain.SubscribeAsync(request);

        // Act
        await grain.UnsubscribeAsync(subscriptionId);

        // Assert
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();
        Assert.Empty(subscriptions);
    }

    /// <summary>
    ///     UnsubscribeAsync with unknown ID should not throw.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnsubscribeAsyncWithUnknownIdDoesNotThrow()
    {
        // Arrange
        UxProjectionSubscriptionGrain grain = CreateGrain(ConnectionId);

        // Act
        await grain.UnsubscribeAsync("unknown-subscription-id");

        // Assert - verify grain still functions and has no subscriptions
        ImmutableList<UxProjectionSubscriptionRequest> subscriptions = await grain.GetSubscriptionsAsync();
        Assert.Empty(subscriptions);
    }
}