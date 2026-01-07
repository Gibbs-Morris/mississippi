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
        // Wait for the channel list container to appear first
        await page.WaitForSelectorAsync(".channel-list", new() { Timeout = 60000 });

        // Wait for the loading state to finish (no "Loading channels..." visible)
        await page.Locator(".channel-list .loading").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60000 });

        // Now wait for either the header button or empty-state button
        ILocator createButton = page.Locator(".channel-list-header button, .channel-list .empty-state button").First;
        await createButton.WaitForAsync(new() { Timeout = 30000 });

        // Click the create channel button
        await createButton.ClickAsync();

        // Wait for modal to appear and fill in the channel name
        await page.WaitForSelectorAsync(".modal input[name='Name'], input[name='Name']");
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