// <copyright file="ChannelListPage.cs" company="Corsair Software Ltd">
// Copyright (c) Corsair Software Ltd. All rights reserved.
// Licensed under the Apache-2.0 License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Playwright;


namespace Cascade.L2Tests.PageObjects;

/// <summary>
///     Page object for the channel list page.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - used by public test classes
internal sealed class ChannelListPage
#pragma warning restore CA1515
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelListPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page.</param>
    public ChannelListPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Creates a new channel with the specified name.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <returns>The channel view page after creation.</returns>
    public async Task<ChannelViewPage> CreateChannelAsync(
        string name
    )
    {
        // Click the create channel button in the header
        await page.ClickAsync(".channel-list-header button");

        // Fill in the channel name
        await page.FillAsync("input[name='Name']", name);

        // Click create button
        await page.ClickAsync("button:has-text('Create')");

        // Wait for the channel to appear in the list
        await page.WaitForSelectorAsync($".channel-item:has-text('{name}')");
        return new(page);
    }

    /// <summary>
    ///     Gets all channel names displayed in the list.
    /// </summary>
    /// <returns>A list of channel names.</returns>
    public async Task<IReadOnlyList<string>> GetChannelNamesAsync()
    {
        IReadOnlyList<IElementHandle> items = await page.QuerySelectorAllAsync(".channel-item .channel-name");
        List<string> names = new();
        foreach (IElementHandle item in items)
        {
            names.Add(await item.TextContentAsync() ?? string.Empty);
        }

        return names;
    }

    /// <summary>
    ///     Checks if any channels are displayed.
    /// </summary>
    /// <returns>True if channels are visible.</returns>
    public async Task<bool> HasChannelsAsync()
    {
        IReadOnlyList<string> channels = await GetChannelNamesAsync();
        return channels.Count > 0;
    }

    /// <summary>
    ///     Selects an existing channel by name.
    /// </summary>
    /// <param name="name">The channel name to select.</param>
    /// <returns>The channel view page.</returns>
    public async Task<ChannelViewPage> SelectChannelAsync(
        string name
    )
    {
        await page.ClickAsync($".channel-item:has-text('{name}')");
        return new(page);
    }
}