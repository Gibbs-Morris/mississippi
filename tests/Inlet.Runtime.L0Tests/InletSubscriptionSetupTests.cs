using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;

using NSubstitute;

using Orleans.Streams;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Verifies failed setup cannot publish an ID the subscriber never receives.
/// </summary>
public sealed class InletSubscriptionSetupTests
{
    private static int NotificationCount(
        InletSubscriptionSetupFixture fixture
    ) =>
        fixture.Client.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(ISignalRClientGrain.SendMessageAsync));

    private static int UnsubscribeCount(
        InletSubscriptionSetupFixture fixture
    ) =>
        fixture.Handle.ReceivedCalls()
            .Count(call =>
                call.GetMethodInfo().Name == nameof(StreamSubscriptionHandle<BrookCursorMovedEvent>.UnsubscribeAsync));

    /// <summary>
    ///     Failing a new brook leaves a successful existing interest and its handle intact.
    /// </summary>
    /// <param name="failCursor">Whether the cursor read fails rather than the stream subscription.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedNewBrookShouldPreserveExistingInterest(
        bool failCursor
    )
    {
        using InletSubscriptionSetupFixture fixture = new();
        string existingId = await fixture.Grain.SubscribeAsync("projection", "entity");
        fixture.FailSetup(failCursor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Grain.SubscribeAsync("other", "entity"));
        ImmutableList<InletSubscription> interests = await fixture.Grain.GetSubscriptionsAsync();
        int releasesBeforeUnsubscribe = UnsubscribeCount(fixture);
        await fixture.Grain.OnNextAsync(new(new BrookKey("TEST", "entity").ToString(), new(4)));
        int notifications = NotificationCount(fixture);
        await fixture.Grain.UnsubscribeAsync(existingId);
        ImmutableList<InletSubscription> remaining = await fixture.Grain.GetSubscriptionsAsync();
        Assert.Multiple(
            () => Assert.Equal(existingId, Assert.Single(interests).SubscriptionId),
            () => Assert.Equal(0, releasesBeforeUnsubscribe),
            () => Assert.Equal(1, notifications),
            () => Assert.Empty(remaining),
            () => Assert.Equal(1, UnsubscribeCount(fixture)));
    }

    /// <summary>
    ///     Failed setup publishes neither an enumerable interest nor a notification recipient.
    /// </summary>
    /// <param name="failCursor">Whether the cursor read fails rather than the stream subscription.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedSetupShouldLeaveNoInterestOrNotification(
        bool failCursor
    )
    {
        using InletSubscriptionSetupFixture fixture = new();
        fixture.FailSetup(failCursor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Grain.SubscribeAsync("projection", "entity"));
        await fixture.Grain.OnNextAsync(new(new BrookKey("TEST", "entity").ToString(), new(4)));
        ImmutableList<InletSubscription> interests = await fixture.Grain.GetSubscriptionsAsync();
        int notifications = NotificationCount(fixture);
        Assert.Multiple(() => Assert.Empty(interests), () => Assert.Equal(0, notifications));
    }

    /// <summary>
    ///     A recovered retry returns one ID, sends one update and releases the sole successful observer.
    /// </summary>
    /// <param name="failCursor">Whether the cursor read fails rather than the stream subscription.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RetryShouldPublishAndReleaseOnlyItsReturnedInterest(
        bool failCursor
    )
    {
        using InletSubscriptionSetupFixture fixture = new();
        fixture.FailSetup(failCursor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Grain.SubscribeAsync("projection", "entity"));
        fixture.RestoreSetup();
        string id = await fixture.Grain.SubscribeAsync("projection", "entity");
        ImmutableList<InletSubscription> interests = await fixture.Grain.GetSubscriptionsAsync();
        string brookKey = new BrookKey("TEST", "entity").ToString();
        await fixture.Grain.OnNextAsync(new(brookKey, new(2)));
        await fixture.Grain.OnNextAsync(new(brookKey, new(4)));
        int notifications = NotificationCount(fixture);
        await fixture.Grain.UnsubscribeAsync(id);
        ImmutableList<InletSubscription> remaining = await fixture.Grain.GetSubscriptionsAsync();
        Assert.Multiple(
            () => Assert.Equal(id, Assert.Single(interests).SubscriptionId),
            () => Assert.Equal(1, notifications),
            () => Assert.Empty(remaining),
            () => Assert.Equal(1, UnsubscribeCount(fixture)));
    }

    /// <summary>
    ///     Successful interests sharing a brook reuse one observer and preserve cursor filtering.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task SharedBrookShouldUseOneHandleAndPreserveCursor()
    {
        using InletSubscriptionSetupFixture fixture = new();
        string first = await fixture.Grain.SubscribeAsync("projection", "entity");
        string second = await fixture.Grain.SubscribeAsync("alternate", "entity");
        Assert.NotEqual(first, second);
        Assert.Equal(2, (await fixture.Grain.GetSubscriptionsAsync()).Count);
        await fixture.Stream.Received(1).SubscribeAsync(Arg.Any<IAsyncObserver<BrookCursorMovedEvent>>());
        string brookKey = new BrookKey("TEST", "entity").ToString();
        await fixture.Grain.OnNextAsync(new(brookKey, new(2)));
        Assert.Equal(0, NotificationCount(fixture));
        await fixture.Grain.OnNextAsync(new(brookKey, new(4)));
        Assert.Equal(2, NotificationCount(fixture));
        await fixture.Client.Received(1)
            .SendMessageAsync(
                InletHubConstants.ProjectionUpdatedMethod,
                Arg.Is<ImmutableArray<object?>>(arguments => (string?)arguments[0] == "projection"));
        await fixture.Client.Received(1)
            .SendMessageAsync(
                InletHubConstants.ProjectionUpdatedMethod,
                Arg.Is<ImmutableArray<object?>>(arguments => (string?)arguments[0] == "alternate"));
        await fixture.Grain.UnsubscribeAsync(first);
        Assert.Equal(0, UnsubscribeCount(fixture));
        await fixture.Grain.UnsubscribeAsync(second);
        Assert.Empty(await fixture.Grain.GetSubscriptionsAsync());
        Assert.Equal(1, UnsubscribeCount(fixture));
    }
}