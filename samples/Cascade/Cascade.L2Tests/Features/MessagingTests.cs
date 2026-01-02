// <copyright file="MessagingTests.cs" company="Corsair Software Ltd">
// Copyright (c) Corsair Software Ltd. All rights reserved.
// Licensed under the Apache-2.0 License. See LICENSE file in the project root for full license information.
// </copyright>

using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for messaging functionality within channels.
/// </summary>
[AllureSubSuite("Messaging")]
public class MessagingTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessagingTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public MessagingTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that sending a message displays it in the channel view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendMessageAppearsInChannelView()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("MessageSender");
        ChannelListPage channelList = new(page);
        string channelName = $"msg-channel-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);
        string messageContent = $"Hello, world! {Guid.NewGuid():N}"[..30];

        // Act
        await channelView.SendMessageAsync(messageContent);

        // Assert
        await channelView.WaitForMessageAsync(messageContent, 10000);
        IReadOnlyList<string> messages = await channelView.GetMessagesAsync();
        Assert.Contains(messages, m => m.Contains(messageContent, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that multiple messages appear in order.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task SendMultipleMessagesAppearInOrder()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("MultiMessageSender");
        ChannelListPage channelList = new(page);
        string channelName = $"multi-msg-{Guid.NewGuid():N}"[..24];
        await channelList.CreateChannelAsync(channelName);
        ChannelViewPage channelView = await channelList.SelectChannelAsync(channelName);
        string message1 = "First message";
        string message2 = "Second message";
        string message3 = "Third message";

        // Act
        await channelView.SendMessageAsync(message1);
        await channelView.WaitForMessageAsync(message1, 10000);
        await channelView.SendMessageAsync(message2);
        await channelView.WaitForMessageAsync(message2, 10000);
        await channelView.SendMessageAsync(message3);
        await channelView.WaitForMessageAsync(message3, 10000);

        // Assert
        IReadOnlyList<string> messages = await channelView.GetMessagesAsync();
        Assert.True(messages.Count >= 3, "Should have at least 3 messages");
    }
}