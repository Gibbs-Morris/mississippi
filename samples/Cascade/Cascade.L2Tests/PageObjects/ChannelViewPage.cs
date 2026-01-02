// <copyright file="ChannelViewPage.cs" company="Corsair Software Ltd">
// Copyright (c) Corsair Software Ltd. All rights reserved.
// Licensed under the Apache-2.0 License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Playwright;


namespace Cascade.L2Tests.PageObjects;

/// <summary>
///     Page object for the channel view page.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - used by public test classes
internal sealed class ChannelViewPage
#pragma warning restore CA1515
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelViewPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page.</param>
    public ChannelViewPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Gets the channel header text.
    /// </summary>
    /// <returns>The channel header text.</returns>
    public async Task<string> GetChannelHeaderAsync()
    {
        IElementHandle? header = await page.QuerySelectorAsync(".channel-header h2");
        if (header is null)
        {
            return string.Empty;
        }

        return await header.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    ///     Gets all messages displayed in the channel.
    /// </summary>
    /// <returns>A list of message contents.</returns>
    public async Task<IReadOnlyList<string>> GetMessagesAsync()
    {
        IReadOnlyList<IElementHandle> items = await page.QuerySelectorAllAsync(".message-item .message-content");
        List<string> messages = new();
        foreach (IElementHandle item in items)
        {
            messages.Add(await item.TextContentAsync() ?? string.Empty);
        }

        return messages;
    }

    /// <summary>
    ///     Checks if the channel view is displayed.
    /// </summary>
    /// <returns>True if the channel view is visible.</returns>
    public async Task<bool> IsVisibleAsync() => await page.IsVisibleAsync(".channel-view");

    /// <summary>
    ///     Sends a message in the channel.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SendMessageAsync(
        string content
    )
    {
        await page.FillAsync(".message-input-field", content);
        await page.ClickAsync(".send-button");
    }

    /// <summary>
    ///     Waits for a message with the specified content to appear.
    /// </summary>
    /// <param name="content">The message content to wait for.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task WaitForMessageAsync(
        string content,
        int timeoutMs = 5000
    )
    {
        await page.WaitForSelectorAsync(
            $".message-item:has-text('{content}')",
            new()
            {
                Timeout = timeoutMs,
            });
    }
}