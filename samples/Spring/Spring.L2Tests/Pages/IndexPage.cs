namespace Spring.L2Tests.Pages;

/// <summary>
///     Page Object Model for the Spring sample Index page.
///     Encapsulates Playwright interactions with the greeting UI.
/// </summary>
public sealed class IndexPage
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IndexPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public IndexPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Clicks the "Say Hello" button and waits for the response.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickSayHelloAsync()
    {
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Say Hello",
                })
            .ClickAsync();
    }

    /// <summary>
    ///     Gets the error message if one is displayed.
    /// </summary>
    /// <returns>The error message text, or null if not present.</returns>
    public async Task<string?> GetErrorMessageAsync()
    {
        ILocator errorLocator = page.Locator("p[style*='color: red']");
        if (await errorLocator.CountAsync() > 0)
        {
            return await errorLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the generated timestamp text.
    /// </summary>
    /// <returns>The timestamp text.</returns>
    public async Task<string?> GetGeneratedAtTextAsync()
    {
        ILocator timestampLocator = page.Locator("p > small");
        if (await timestampLocator.CountAsync() > 0)
        {
            return await timestampLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the greeting text displayed on the page.
    /// </summary>
    /// <returns>The greeting text.</returns>
    public async Task<string?> GetGreetingTextAsync()
    {
        // The greeting is in a <p> element but not the one with <small> or error style
        // Look for paragraph that contains the greeting pattern
        ILocator greetingLocator = page.Locator("p")
            .Filter(
                new()
                {
                    HasText = "Hello",
                });
        if (await greetingLocator.CountAsync() > 0)
        {
            return await greetingLocator.First.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the page title (h1 element).
    /// </summary>
    /// <returns>The page title.</returns>
    public async Task<string?> GetTitleAsync() => await page.Locator("h1").TextContentAsync();

    /// <summary>
    ///     Checks if the loading indicator is visible.
    /// </summary>
    /// <returns>True if loading is visible.</returns>
    public async Task<bool> IsLoadingVisibleAsync()
    {
        ILocator loadingLocator = page.Locator("p")
            .Filter(
                new()
                {
                    HasText = "Loading...",
                });
        return await loadingLocator.CountAsync() > 0;
    }

    /// <summary>
    ///     Navigates to the index page.
    /// </summary>
    /// <param name="baseUri">The base URI of the application.</param>
    /// <returns>A task representing the navigation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseUri" /> is null.</exception>
    public async Task NavigateAsync(
        Uri baseUri
    )
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        await page.GotoAsync(
            baseUri.ToString(),
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });
    }

    /// <summary>
    ///     Waits for the greeting to appear on the page.
    /// </summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForGreetingAsync(
        float? timeout = null
    )
    {
        await page.Locator("p")
            .Filter(
                new()
                {
                    HasText = "Hello",
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });
    }
}