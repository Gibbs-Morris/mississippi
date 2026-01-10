using System.Diagnostics;

using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for real-time functionality using multiple browser contexts.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Real-Time")]
[AllureSubSuite("Message Delivery")]
public class RealTimeTests : TestBase
{
    /// <summary>
    ///     Maximum channel name length for test channels.
    ///     Truncated to 24 chars to ensure uniqueness while keeping names readable.
    ///     Format: prefix (8-10 chars) + partial GUID (14-16 chars) = 24 total.
    /// </summary>
    private const int MaxChannelNameLength = 24;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RealTimeTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public RealTimeTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that real-time updates work with message delivery under 5 seconds.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task RealTimeDeliveryUnderFiveSeconds()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("SpeedTester");
        ChannelListPage channelList = new(page);
        string channelName = $"speed-test-{Guid.NewGuid():N}"[..MaxChannelNameLength];
        await channelList.CreateChannelAsync(channelName);
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);
        string messageContent = $"Speed test {DateTime.UtcNow:HH:mm:ss.fff}";

        // Act & Assert: Message should appear within 5 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();
        await channelView.SendMessageAsync(messageContent);
        await channelView.WaitForMessageAsync(messageContent);
        stopwatch.Stop();
        Assert.True(
            stopwatch.ElapsedMilliseconds < 5000,
            $"Message delivery took {stopwatch.ElapsedMilliseconds}ms, expected under 5000ms");
    }

    /// <summary>
    ///     Verifies that a message sent by one user appears in another user's view in real-time.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendMessageAppearsInUserViewAfterSending()
    {
        // Arrange: Two users in same channel
        IPage pageAlice = await CreatePageAndLoginAsync("Alice");

        // Alice creates channel
        ChannelListPage aliceChannelList = new(pageAlice);
        string channelName = $"realtime-{Guid.NewGuid():N}"[..MaxChannelNameLength];
        await aliceChannelList.CreateChannelAsync(channelName);
        ChannelViewPage aliceChannelView = await aliceChannelList.SelectChannelAsync(channelName);

        // Test that Alice's message appears in her own view
        string messageContent = "Hello from Alice!";

        // Act: Alice sends message
        await aliceChannelView.SendMessageAsync(messageContent);

        // Assert: Message appears in Alice's view
        await aliceChannelView.WaitForMessageAsync(messageContent);
        IReadOnlyList<string> aliceMessages = await aliceChannelView.GetMessagesAsync();
        Assert.Contains(aliceMessages, m => m.Contains("Hello from Alice!", StringComparison.Ordinal));
    }
}