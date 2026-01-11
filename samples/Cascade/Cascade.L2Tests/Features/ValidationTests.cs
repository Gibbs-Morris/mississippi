using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for input validation and error handling.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Validation")]
[AllureSubSuite("Input Validation")]
public class ValidationTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidationTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ValidationTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that channel name input accepts text.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelNameInputAcceptsText()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("InputTestUser");

        // Open create channel modal
        await page.ClickAsync(".channel-list-header button");
        await page.WaitForSelectorAsync(
            "input[name='Name']",
            new()
            {
                Timeout = 5000,
            });

        // Act
        string channelName = "test-channel-name";
        await page.FillAsync("input[name='Name']", channelName);

        // Assert
        string value = await page.InputValueAsync("input[name='Name']");
        Assert.Equal(channelName, value);
    }

    /// <summary>
    ///     Verifies that login with empty name shows validation error.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task LoginWithEmptyNameShowsValidation()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();
        await page.GotoAsync(Fixture.BaseUrl + "/login");

        // Act - try to submit with empty name
        await page.FillAsync("#displayName", string.Empty);
        await page.ClickAsync("button[type='submit']");

        // Assert - should still be on login page (validation prevents submission)
        // Wait for any navigation to complete, then verify URL
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/login", page.Url, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that message input accepts text.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task MessageInputAcceptsText()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("MsgInputUser");
        ChannelListPage channelList = new(page);
        string channelName = $"msginput-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);
        await page.WaitForSelectorAsync(
            ".message-input-field",
            new()
            {
                Timeout = 5000,
            });

        // Act
        string messageText = "Test message content";
        await page.FillAsync(".message-input-field", messageText);

        // Assert
        string value = await page.InputValueAsync(".message-input-field");
        Assert.Equal(messageText, value);
    }

    /// <summary>
    ///     Verifies that message clears after sending.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task MessageInputClearsAfterSend()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ClearInputUser");
        ChannelListPage channelList = new(page);
        string channelName = $"clearinput-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);

        // Act - send a message
        string messageContent = "Test message to send";
        await channelView.SendMessageAsync(messageContent);
        await channelView.WaitForMessageAsync(messageContent, 10000);

        // Assert - input should be cleared
        string value = await page.InputValueAsync(".message-input-field");
        Assert.Equal(string.Empty, value);
    }

    /// <summary>
    ///     Verifies that send button is initially disabled with empty message.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendButtonDisabledWithEmptyMessage()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("DisabledBtnUser");
        ChannelListPage channelList = new(page);
        string channelName = $"disabled-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);

        // Assert - send button should be disabled
        await page.WaitForSelectorAsync(
            ".send-button",
            new()
            {
                Timeout = 5000,
            });
        bool isDisabled = await page.IsDisabledAsync(".send-button");
        Assert.True(isDisabled, "Send button should be disabled with empty message");
    }

    /// <summary>
    ///     Verifies that send button enables when message is entered.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendButtonEnabledWithMessage()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("EnabledBtnUser");
        ChannelListPage channelList = new(page);
        string channelName = $"enabled-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);
        await page.WaitForSelectorAsync(
            ".message-input-field",
            new()
            {
                Timeout = 5000,
            });

        // Act - enter a message
        await page.FillAsync(".message-input-field", "Test message");

        // Wait for send button to become enabled
        await page.WaitForSelectorAsync(
            ".send-button:not([disabled])",
            new()
            {
                Timeout = 5000,
            });

        // Assert - send button should be enabled
        bool isDisabled = await page.IsDisabledAsync(".send-button");
        Assert.False(isDisabled, "Send button should be enabled with message");
    }
}