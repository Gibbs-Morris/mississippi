using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for multiple channels and switching behavior.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Channels")]
[AllureSubSuite("Multi-Channel")]
public class MultiChannelTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MultiChannelTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public MultiChannelTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that multiple channels can be created.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task CanCreateMultipleChannels()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("MultiChannelUser");
        ChannelListPage channelList = new(page);
        string channel1 = $"channel1-{Guid.NewGuid():N}"[..24];
        string channel2 = $"channel2-{Guid.NewGuid():N}"[..24];
        string channel3 = $"channel3-{Guid.NewGuid():N}"[..24];

        // Act
        await channelList.CreateChannelAsync(channel1);
        await channelList.CreateChannelAsync(channel2);
        await channelList.CreateChannelAsync(channel3);

        // Assert
        IReadOnlyList<string> channels = await channelList.GetChannelNamesAsync();
        Assert.Contains(channel1, channels);
        Assert.Contains(channel2, channels);
        Assert.Contains(channel3, channels);
    }

    /// <summary>
    ///     Verifies that channel list shows count correctly.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelListShowsCorrectCount()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("CountUser");
        ChannelListPage channelList = new(page);

        // Create 3 channels
        string[] channels =
        [
            $"count1-{Guid.NewGuid():N}"[..24],
            $"count2-{Guid.NewGuid():N}"[..24],
            $"count3-{Guid.NewGuid():N}"[..24],
        ];
        foreach (string channel in channels)
        {
            await channelList.CreateChannelAsync(channel);
        }

        // Assert
        IReadOnlyList<string> channelNames = await channelList.GetChannelNamesAsync();
        Assert.True(channelNames.Count >= 3, $"Expected at least 3 channels, found {channelNames.Count}");
    }

    /// <summary>
    ///     Verifies that channel selection is preserved when returning.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelMessagesPersistWhenReturning()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("PersistUser");
        ChannelListPage channelList = new(page);
        string channel1 = $"persist1-{Guid.NewGuid():N}"[..24];
        string channel2 = $"persist2-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channel1);
        await channelList.CreateChannelAsync(channel2);

        // Send message to channel 1
        ChannelViewPage view1 = await channelList.SelectChannelAsync(channel1);
        string message1 = "Persistent message";
        await view1.SendMessageAsync(message1);
        await view1.WaitForMessageAsync(message1, 10000);

        // Switch to channel 2 and back to channel 1
        await channelList.SelectChannelAsync(channel2);
        await Task.Delay(500); // Wait for view switch
        await channelList.SelectChannelAsync(channel1);
        await view1.WaitForMessageAsync(message1, 10000);

        // Assert - message should still be visible
        IReadOnlyList<string> messages = await view1.GetMessagesAsync();
        Assert.Contains(messages, m => m.Contains(message1, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that messages are isolated per channel.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task MessagesAreIsolatedPerChannel()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("IsolationUser");
        ChannelListPage channelList = new(page);
        string channel1 = $"iso1-{Guid.NewGuid():N}"[..24];
        string channel2 = $"iso2-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channel1);
        await channelList.CreateChannelAsync(channel2);

        // Send message to channel 1
        ChannelViewPage view1 = await channelList.SelectChannelAsync(channel1);
        string message1 = "Message for channel 1 only";
        await view1.SendMessageAsync(message1);
        await view1.WaitForMessageAsync(message1, 10000);

        // Switch to channel 2
        ChannelViewPage view2 = await channelList.SelectChannelAsync(channel2);
        await page.WaitForSelectorAsync(
            ".channel-view",
            new()
            {
                Timeout = 5000,
            });

        // Assert - message1 should not be visible in channel 2
        IReadOnlyList<string> channel2Messages = await view2.GetMessagesAsync();
        Assert.DoesNotContain(channel2Messages, m => m.Contains(message1, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that switching channels updates the view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SwitchingChannelsUpdatesView()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("SwitchUser");
        ChannelListPage channelList = new(page);
        string channel1 = $"switch1-{Guid.NewGuid():N}"[..24];
        string channel2 = $"switch2-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channel1);
        await channelList.CreateChannelAsync(channel2);

        // Act - switch between channels
        await channelList.SelectChannelAsync(channel1);
        ChannelViewPage view1 = new(page);
        string header1 = await view1.GetChannelHeaderAsync();
        await channelList.SelectChannelAsync(channel2);
        ChannelViewPage view2 = new(page);
        string header2 = await view2.GetChannelHeaderAsync();

        // Assert
        Assert.Contains(channel1, header1, StringComparison.Ordinal);
        Assert.Contains(channel2, header2, StringComparison.Ordinal);
    }
}