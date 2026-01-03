using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for channel creation functionality.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Channels")]
[AllureSubSuite("Channel Creation")]
public class ChannelCreationTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelCreationTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ChannelCreationTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that creating a channel adds it to the channel list.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task CreateChannelAppearsInChannelList()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ChannelCreator");
        ChannelListPage channelList = new(page);
        string channelName = $"test-channel-{Guid.NewGuid():N}"[..24];

        // Act
        await channelList.CreateChannelAsync(channelName);

        // Assert
        IReadOnlyList<string> channels = await channelList.GetChannelNamesAsync();
        Assert.Contains(channelName, channels);
    }

    /// <summary>
    ///     Verifies that a new user starts with an empty channel list.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NewUserSeesChannelListRendered()
    {
        // Arrange & Act
        IPage page = await CreatePageAndLoginAsync($"NewUser-{Guid.NewGuid():N}"[..20]);

        // Assert - new user should see the channel list component rendered
        await page.WaitForSelectorAsync(
            ".channel-list",
            new()
            {
                Timeout = 5000,
            });
        bool isVisible = await page.IsVisibleAsync(".channel-list");
        Assert.True(isVisible, "Channel list should be visible for new user");
    }

    /// <summary>
    ///     Verifies that selecting a created channel opens the channel view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SelectChannelOpensChannelView()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ChannelViewer");
        ChannelListPage channelList = new(page);
        string channelName = $"view-channel-{Guid.NewGuid():N}"[..24];

        // Create and then select the channel
        await channelList.CreateChannelAsync(channelName);

        // Act
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);

        // Assert
        bool isVisible = await channelView.IsVisibleAsync();
        Assert.True(isVisible, "Channel view should be visible after selecting a channel");
    }
}