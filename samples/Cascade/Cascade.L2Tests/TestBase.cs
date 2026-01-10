using Microsoft.Playwright;


namespace Cascade.L2Tests;

/// <summary>
///     Base class for E2E tests providing common helper methods.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit requires public base class
[Collection("Cascade L2 Tests")]
public abstract class TestBase
#pragma warning restore CA1515
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestBase" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    protected TestBase(
        PlaywrightFixture fixture
    ) =>
        Fixture = fixture;

    /// <summary>
    ///     Gets the Playwright fixture with browser and AppHost.
    /// </summary>
    protected PlaywrightFixture Fixture { get; }

    /// <summary>
    ///     Creates a new page and logs in with the specified display name.
    /// </summary>
    /// <param name="displayName">The display name to use for login.</param>
    /// <returns>The logged-in page.</returns>
    protected async Task<IPage> CreatePageAndLoginAsync(
        string displayName
    )
    {
        IPage page = await Fixture.CreatePageAsync();

        // Navigate directly to login page with extended timeout for Blazor Server startup
        await page.GotoAsync(
            Fixture.BaseUrl + "/login",
            new()
            {
                Timeout = 60000,
                WaitUntil = WaitUntilState.DOMContentLoaded,
            });

        // Wait for login page to load completely (including Blazor circuit)
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new()
            {
                Timeout = 60000,
            });
        await page.WaitForSelectorAsync(
            "#displayName",
            new()
            {
                Timeout = 60000,
            });

        // Wait for the submit button to become enabled to ensure the login form is interactive
        await page.WaitForSelectorAsync(
            "button[type='submit']:not([disabled])",
            new()
            {
                Timeout = 60000,
            });

        // Enter display name
        await page.FillAsync("#displayName", displayName);

        // Click submit button
        await page.ClickAsync("button[type='submit']");

        // Wait for Blazor to process the form and navigate by waiting for network to become idle
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new()
            {
                Timeout = 60000,
            });

        // Take debug screenshot to see what happened after submit
        string debugPath = Path.Combine(Path.GetTempPath(), $"cascade-login-{Guid.NewGuid()}.png");
        await page.ScreenshotAsync(
            new()
            {
                Path = debugPath,
                FullPage = true,
            });

        // Wait for the channels page to render with the chat container and channel list
        // This confirms: 1) navigation happened, 2) user is authenticated, 3) ChannelList rendered
        await page.WaitForSelectorAsync(
            ".chat-container",
            new()
            {
                Timeout = 60000,
            });

        // Verify the channel list component is present (inside chat-container)
        await page.WaitForSelectorAsync(
            ".channel-list",
            new()
            {
                Timeout = 60000,
            });
        return page;
    }
}