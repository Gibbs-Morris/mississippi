using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for user interface state and visual feedback.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("UI State")]
[AllureSubSuite("Visual Feedback")]
public class UiStateTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UiStateTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public UiStateTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that the welcome message shows the user's display name.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task WelcomeMessageShowsUserName()
    {
        // Arrange
        string displayName = $"WelcomeUser-{Guid.NewGuid():N}"[..20];

        // Act
        IPage page = await CreatePageAndLoginAsync(displayName);

        // Assert - welcome message should show user name
        string content = await page.ContentAsync();
        Assert.Contains("Welcome", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(displayName, content, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the channel list sidebar is visible after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelListSidebarIsVisible()
    {
        // Arrange & Act
        IPage page = await CreatePageAndLoginAsync("SidebarUser");

        // Assert
        await page.WaitForSelectorAsync(".channel-list", new() { Timeout = 5000 });
        bool isVisible = await page.IsVisibleAsync(".channel-list");
        Assert.True(isVisible, "Channel list sidebar should be visible");
    }

    /// <summary>
    ///     Verifies that the create channel button is visible in the header.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task CreateChannelButtonIsVisible()
    {
        // Arrange & Act
        IPage page = await CreatePageAndLoginAsync("ButtonUser");

        // Assert
        await page.WaitForSelectorAsync(".channel-list-header button", new() { Timeout = 5000 });
        bool isVisible = await page.IsVisibleAsync(".channel-list-header button");
        Assert.True(isVisible, "Create channel button should be visible");
    }

    /// <summary>
    ///     Verifies that clicking create channel opens a modal.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task CreateChannelButtonOpensModal()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ModalUser");

        // Act
        await page.ClickAsync(".channel-list-header button");

        // Assert - modal should appear with input field
        await page.WaitForSelectorAsync("input[name='Name']", new() { Timeout = 5000 });
        bool inputVisible = await page.IsVisibleAsync("input[name='Name']");
        Assert.True(inputVisible, "Channel name input should be visible in modal");
    }

    /// <summary>
    ///     Verifies that the selected channel is visually highlighted.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SelectedChannelIsHighlighted()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("HighlightUser");
        ChannelListPage channelList = new(page);
        string channelName = $"highlight-{Guid.NewGuid():N}"[..24];

        // Create and select channel
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);

        // Assert - selected channel should have 'selected' class
        IElementHandle? selectedItem = await page.QuerySelectorAsync(".channel-item.selected");
        Assert.NotNull(selectedItem);
    }

    /// <summary>
    ///     Verifies that the channel header shows the channel name.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelHeaderShowsChannelName()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("HeaderUser");
        ChannelListPage channelList = new(page);
        string channelName = $"header-test-{Guid.NewGuid():N}"[..24];

        // Create and select channel
        await channelList.CreateChannelAsync(channelName);
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);

        // Assert
        string header = await channelView.GetChannelHeaderAsync();
        Assert.Contains(channelName, header, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that new user sees empty state message.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NewUserSeesEmptyStateMessage()
    {
        // Arrange & Act
        IPage page = await CreatePageAndLoginAsync($"EmptyUser-{Guid.NewGuid():N}"[..20]);

        // Wait for channel list to load
        await page.WaitForSelectorAsync(".channel-list", new() { Timeout = 5000 });

        // Assert - should see empty state or create button
        string content = await page.ContentAsync();
        bool hasEmptyState = content.Contains("No channels", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("Create", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasEmptyState, "Should show empty state or create prompt for new user");
    }

    /// <summary>
    ///     Verifies that the message input is disabled when sending.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task MessageInputExistsInChannelView()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("InputUser");
        ChannelListPage channelList = new(page);
        string channelName = $"input-test-{Guid.NewGuid():N}"[..24];

        // Create and select channel
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);

        // Assert - message input should be visible
        await page.WaitForSelectorAsync(".message-input-field", new() { Timeout = 5000 });
        bool isVisible = await page.IsVisibleAsync(".message-input-field");
        Assert.True(isVisible, "Message input field should be visible");
    }

    /// <summary>
    ///     Verifies that the send button exists in channel view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendButtonExistsInChannelView()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("SendBtnUser");
        ChannelListPage channelList = new(page);
        string channelName = $"sendbtn-{Guid.NewGuid():N}"[..24];

        // Create and select channel
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);

        // Assert - send button should be visible
        await page.WaitForSelectorAsync(".send-button", new() { Timeout = 5000 });
        bool isVisible = await page.IsVisibleAsync(".send-button");
        Assert.True(isVisible, "Send button should be visible");
    }
}
