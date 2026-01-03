using Microsoft.Playwright;


namespace Cascade.L2Tests;

/// <summary>
///     Base class for E2E tests providing common helper methods.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit requires public base class
public abstract class TestBase : IClassFixture<PlaywrightFixture>
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
        await page.GotoAsync(Fixture.BaseUrl);

        // Should redirect to login
        await page.WaitForURLAsync("**/login");

        // Enter display name
        await page.FillAsync("[id='displayName']", displayName);
        await page.ClickAsync("button[type='submit']");

        // Wait for redirect to channels
        await page.WaitForURLAsync("**/channels");
        return page;
    }
}